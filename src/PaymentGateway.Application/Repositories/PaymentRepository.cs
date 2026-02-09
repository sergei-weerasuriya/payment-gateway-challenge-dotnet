using System.Collections.Concurrent;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Encryption;
using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Application.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<Guid, StoredPayment> _payments = new();
    private readonly IEncryptionService _encryptionService;

    public PaymentRepository(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public Task<bool> CreateAsync(CreatePaymentDto paymentDto)
    {
        var storedPayment = ToStoredPayment(paymentDto);
        var result = _payments.TryAdd(paymentDto.Id, storedPayment);
        return Task.FromResult(result);
    }

    public Task<PaymentDto?> GetByIdAsync(Guid id, Guid merchantId)
    {
        if (_payments.TryGetValue(id, out var storedPayment) && storedPayment.MerchantId == merchantId)
        {
            var paymentDto = ToPaymentDto(storedPayment);
            return Task.FromResult<PaymentDto?>(paymentDto);
        }

        return Task.FromResult<PaymentDto?>(null);
    }

    private StoredPayment ToStoredPayment(CreatePaymentDto dto)
    {
        return new StoredPayment
        {
            Id = dto.Id,
            Status = dto.Status,
            EncryptedCardNumber = _encryptionService.Encrypt(dto.CardNumber),
            CardLastFour = dto.CardLastFour,
            ExpiryMonth = dto.ExpiryMonth,
            ExpiryYear = dto.ExpiryYear,
            EncryptedCvv = _encryptionService.Encrypt(dto.Cvv),
            Currency = dto.Currency,
            Amount = dto.Amount,
            AuthorizationCode = dto.AuthorizationCode,
            CreatedAt = dto.CreatedAt,
            MerchantId = dto.MerchantId
        };
    }

    private PaymentDto ToPaymentDto(StoredPayment stored)
    {
        return new PaymentDto
        {
            Id = stored.Id,
            Status = stored.Status,
            CardNumber = _encryptionService.Decrypt(stored.EncryptedCardNumber),
            CardLastFour = stored.CardLastFour,
            ExpiryMonth = stored.ExpiryMonth,
            ExpiryYear = stored.ExpiryYear,
            Cvv = _encryptionService.Decrypt(stored.EncryptedCvv),
            Currency = stored.Currency,
            Amount = stored.Amount,
            AuthorizationCode = stored.AuthorizationCode,
            CreatedAt = stored.CreatedAt,
            MerchantId = stored.MerchantId
        };
    }
}