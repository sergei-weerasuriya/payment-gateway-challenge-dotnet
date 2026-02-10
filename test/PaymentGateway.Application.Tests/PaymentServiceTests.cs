using System.Diagnostics.Metrics;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentGateway.Application.BankClient;
using PaymentGateway.Application.Common;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Metrics;
using PaymentGateway.Application.Services;
using PaymentGateway.Contracts.Models;
using Xunit;

namespace PaymentGateway.Application.Tests;

public class PaymentServiceTests : IDisposable
{
    private readonly Mock<IValidator<ProcessPaymentCommand>> _validatorMock;
    private readonly Mock<IBankClient> _bankClientMock;
    private readonly Mock<IPaymentRepository> _repositoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly IMeterFactory _meterFactory;
    private readonly PaymentMetrics _metrics;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _validatorMock = new Mock<IValidator<ProcessPaymentCommand>>();
        _bankClientMock = new Mock<IBankClient>();
        _repositoryMock = new Mock<IPaymentRepository>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        _meterFactory = new TestMeterFactory();
        _metrics = new PaymentMetrics(_meterFactory);

        _sut = new PaymentService(
            _validatorMock.Object,
            _bankClientMock.Object,
            _repositoryMock.Object,
            _loggerMock.Object,
            _metrics);

        // Default: validation passes
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    public void Dispose()
    {
        _metrics.Dispose();
        _meterFactory.Dispose();
    }

    private static ProcessPaymentCommand CreateValidCommand() => new()
    {
        CardNumber = "4111111111111111",
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Cvv = "123",
        Currency = "USD",
        Amount = 1000
    };

    private static readonly Guid MerchantId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    #region ProcessPaymentAsync - Validation

    [Fact]
    public async Task ProcessPayment_WhenValidationFails_ReturnsRejected()
    {
        // Arrange
        var command = CreateValidCommand();
        var validationFailures = new List<ValidationFailure>
        {
            new("CardNumber", "Card number is invalid.")
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        var result = await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Match(
            _ => throw new Exception("Expected failure"),
            rejected =>
            {
                rejected.Errors.Should().ContainKey("CardNumber");
                return rejected;
            });
    }

    [Fact]
    public async Task ProcessPayment_WhenValidationFails_DoesNotCallBank()
    {
        // Arrange
        var command = CreateValidCommand();
        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Amount", "Invalid") }));

        // Act
        await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        _bankClientMock.Verify(
            b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPayment_WhenValidationFails_DoesNotStorePayment()
    {
        // Arrange
        var command = CreateValidCommand();
        _validatorMock
            .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Cvv", "Invalid") }));

        // Act
        await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        _repositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<CreatePaymentDto>()),
            Times.Never);
    }

    #endregion

    #region ProcessPaymentAsync - Bank Authorized

    [Fact]
    public async Task ProcessPayment_WhenBankAuthorizes_ReturnsAuthorizedResult()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupBankAuthorized();

        // Act
        var result = await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var paymentResult = result.GetValueOrThrow();
        paymentResult.Status.Should().Be(PaymentStatus.Authorized);
        paymentResult.Amount.Should().Be(command.Amount);
        paymentResult.Currency.Should().Be("USD");
        paymentResult.ExpiryMonth.Should().Be(command.ExpiryMonth);
        paymentResult.ExpiryYear.Should().Be(command.ExpiryYear);
    }

    [Fact]
    public async Task ProcessPayment_WhenBankAuthorizes_MasksCardNumber()
    {
        // Arrange
        var command = CreateValidCommand() with { CardNumber = "4111111111111111" };
        SetupBankAuthorized();

        // Act
        var result = await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        var paymentResult = result.GetValueOrThrow();
        paymentResult.CardNumberLastFour.Should().Be("1111");
    }

    [Fact]
    public async Task ProcessPayment_WhenBankAuthorizes_StoresPayment()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupBankAuthorized();

        // Act
        await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        _repositoryMock.Verify(
            r => r.CreateAsync(It.Is<CreatePaymentDto>(dto =>
                dto.Status == PaymentStatus.Authorized &&
                dto.Amount == command.Amount &&
                dto.Currency == "USD" &&
                dto.MerchantId == MerchantId &&
                dto.CardLastFour == "1111")),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_WhenBankAuthorizes_NormalizesCurrency()
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = "usd" };
        SetupBankAuthorized();

        // Act
        var result = await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        var paymentResult = result.GetValueOrThrow();
        paymentResult.Currency.Should().Be("USD");
    }

    #endregion

    #region ProcessPaymentAsync - Bank Declined

    [Fact]
    public async Task ProcessPayment_WhenBankDeclines_ReturnsDeclinedResult()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupBankDeclined();

        // Act
        var result = await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var paymentResult = result.GetValueOrThrow();
        paymentResult.Status.Should().Be(PaymentStatus.Declined);
    }

    [Fact]
    public async Task ProcessPayment_WhenBankDeclines_StoresPayment()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupBankDeclined();

        // Act
        await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        _repositoryMock.Verify(
            r => r.CreateAsync(It.Is<CreatePaymentDto>(dto =>
                dto.Status == PaymentStatus.Declined)),
            Times.Once);
    }

    #endregion

    #region ProcessPaymentAsync - Bank Unavailable

    [Fact]
    public async Task ProcessPayment_WhenBankUnavailable_ReturnsRejected()
    {
        // Arrange
        var command = CreateValidCommand();
        _bankClientMock
            .Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankResponse, PaymentRejected>.Failure(PaymentRejected.BankUnavailable()));

        // Act
        var result = await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessPayment_WhenBankUnavailable_DoesNotStorePayment()
    {
        // Arrange
        var command = CreateValidCommand();
        _bankClientMock
            .Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankResponse, PaymentRejected>.Failure(PaymentRejected.BankUnavailable()));

        // Act
        await _sut.ProcessPaymentAsync(command, MerchantId);

        // Assert
        _repositoryMock.Verify(
            r => r.CreateAsync(It.IsAny<CreatePaymentDto>()),
            Times.Never);
    }

    #endregion

    #region GetPaymentAsync

    [Fact]
    public async Task GetPayment_WhenFound_ReturnsPaymentResult()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var paymentDto = new PaymentDto
        {
            Id = paymentId,
            Status = PaymentStatus.Authorized,
            CardNumber = "4111111111111111",
            CardLastFour = "1111",
            ExpiryMonth = 12,
            ExpiryYear = 2027,
            Cvv = "123",
            Currency = "USD",
            Amount = 1000,
            AuthorizationCode = "AUTH123",
            CreatedAt = DateTime.UtcNow,
            MerchantId = MerchantId
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(paymentId, MerchantId))
            .ReturnsAsync(paymentDto);

        // Act
        var result = await _sut.GetPaymentAsync(paymentId, MerchantId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(paymentId);
        result.Status.Should().Be(PaymentStatus.Authorized);
        result.CardNumberLastFour.Should().Be("1111");
        result.Amount.Should().Be(1000);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetPayment_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(paymentId, MerchantId))
            .ReturnsAsync((PaymentDto?)null);

        // Act
        var result = await _sut.GetPaymentAsync(paymentId, MerchantId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPayment_WithWrongMerchant_ReturnsNull()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var wrongMerchantId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(paymentId, wrongMerchantId))
            .ReturnsAsync((PaymentDto?)null);

        // Act
        var result = await _sut.GetPaymentAsync(paymentId, wrongMerchantId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helpers

    private void SetupBankAuthorized()
    {
        _bankClientMock
            .Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankResponse, PaymentRejected>.Success(
                new BankResponse { Authorized = true, AuthorizationCode = "AUTH123" }));
    }

    private void SetupBankDeclined()
    {
        _bankClientMock
            .Setup(b => b.AuthorizePaymentAsync(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<BankResponse, PaymentRejected>.Success(
                new BankResponse { Authorized = false, AuthorizationCode = "" }));
    }

    /// <summary>
    /// Simple IMeterFactory implementation for tests.
    /// </summary>
    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
                meter.Dispose();
            _meters.Clear();
        }
    }

    #endregion
}