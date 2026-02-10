using FluentValidation;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.BankClient;
using PaymentGateway.Application.Common;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Mapping;
using PaymentGateway.Application.Metrics;
using PaymentGateway.Application.Models;
using PaymentGateway.Contracts.Models;

namespace PaymentGateway.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IValidator<ProcessPaymentCommand> _validator;
    private readonly IBankClient _bankClient;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentMetrics _metrics;

    public PaymentService(IValidator<ProcessPaymentCommand> validator, IBankClient bankClient, IPaymentRepository paymentRepository, ILogger<PaymentService> logger, PaymentMetrics metrics)
    {
        _validator = validator;
        _bankClient = bankClient;
        _paymentRepository = paymentRepository;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<Result<PaymentResult, PaymentRejected>> ProcessPaymentAsync(ProcessPaymentCommand command, Guid merchantId, CancellationToken cancellationToken = default)
    {
        using var timer = _metrics.StartPaymentTimer(command.Currency);

        _logger.LogInformation("Processing payment request");

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Payment request rejected due to validation errors: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            timer.SetStatus("rejected");
            _metrics.RecordPaymentRejected();

            return PaymentRejected.FromFieldErrors(errors);
        }

        var bankRequest = CreateBankRequest(command);
        var bankResult = await _bankClient.AuthorizePaymentAsync(bankRequest, cancellationToken);

        if (bankResult.IsFailure)
        {
            _logger.LogWarning("Payment rejected: Bank unavailable");
            timer.SetStatus("bank_error");
            _metrics.RecordBankError();

            return bankResult.Match<Result<PaymentResult, PaymentRejected>>(
                _ => throw new InvalidOperationException("Expected failure"),
                rejected => rejected);
        }

        var bankResponse = bankResult.GetValueOrThrow();

        var payment = CreatePayment(command, bankResponse, merchantId);
        await _paymentRepository.CreateAsync(payment.ToCreatePaymentDto());

        _metrics.RecordPaymentProcessed(command.Currency);
        if (payment.Status == PaymentStatus.Authorized)
        {
            timer.SetStatus("authorized");
            _metrics.RecordPaymentAuthorized(command.Currency);
        }
        else
        {
            timer.SetStatus("declined");
            _metrics.RecordPaymentDeclined(command.Currency);
        }

        _logger.LogInformation("Payment {PaymentId} processed with status {Status}", payment.Id, payment.Status);

        return payment.ToPaymentResult();
    }

    public async Task<PaymentResult?> GetPaymentAsync(Guid paymentId, Guid merchantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving payment {PaymentId} for merchant {MerchantId}", paymentId, merchantId);

        var paymentDto = await _paymentRepository.GetByIdAsync(paymentId, merchantId);

        if (paymentDto is null)
        {
            _logger.LogWarning("Payment {PaymentId} not found for merchant {MerchantId}", paymentId, merchantId);
            return null;
        }

        return paymentDto.ToPaymentResult();
    }

    private static BankRequest CreateBankRequest(ProcessPaymentCommand command)
    {
        var expiryDate = $"{command.ExpiryMonth:D2}/{command.ExpiryYear}";

        return new BankRequest
        {
            CardNumber = command.CardNumber,
            ExpiryDate = expiryDate,
            Currency = command.Currency.ToUpperInvariant(),
            Amount = command.Amount,
            Cvv = command.Cvv
        };
    }

    private static Payment CreatePayment(ProcessPaymentCommand command, BankResponse bankResponse, Guid merchantId)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            Status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
            Card = new Card
            {
                Number = command.CardNumber,
                ExpiryMonth = command.ExpiryMonth,
                ExpiryYear = command.ExpiryYear,
                Cvv = command.Cvv
            },
            Currency = command.Currency.ToUpperInvariant(),
            Amount = command.Amount,
            AuthorizationCode = bankResponse.Authorized ? bankResponse.AuthorizationCode : null,
            MerchantId = merchantId
        };
    }
}