using FluentAssertions;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Validation;
using Xunit;

namespace PaymentGateway.Application.Tests;

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _sut = new();

    private static ProcessPaymentCommand CreateValidCommand() => new()
    {
        CardNumber = "4111111111111111",
        ExpiryMonth = DateTime.UtcNow.Month,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Cvv = "123",
        Currency = "USD",
        Amount = 1000
    };

    #region Card Number

    [Fact]
    public async Task Validate_WithValidCardNumber_Passes()
    {
        var command = CreateValidCommand() with { CardNumber = "4111111111111111" };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyOrNullCardNumber_Fails(string? cardNumber)
    {
        var command = CreateValidCommand() with { CardNumber = cardNumber! };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CardNumber");
    }

    [Theory]
    [InlineData("1234567890123")]   // 13 digits - too short
    [InlineData("12345678901234567890")] // 20 digits - too long
    public async Task Validate_WithInvalidLengthCardNumber_Fails(string cardNumber)
    {
        var command = CreateValidCommand() with { CardNumber = cardNumber };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CardNumber");
    }

    [Theory]
    [InlineData("4111-1111-1111-1111")]
    [InlineData("41111111111111ab")]
    public async Task Validate_WithNonNumericCardNumber_Fails(string cardNumber)
    {
        var command = CreateValidCommand() with { CardNumber = cardNumber };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "CardNumber" &&
            e.ErrorMessage.Contains("numeric"));
    }

    [Theory]
    [InlineData("12345678901234")]   // 14 digits - min
    [InlineData("1234567890123456789")] // 19 digits - max
    public async Task Validate_WithBoundaryLengthCardNumber_Passes(string cardNumber)
    {
        var command = CreateValidCommand() with { CardNumber = cardNumber };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Expiry Month

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public async Task Validate_WithValidExpiryMonth_Passes(int month)
    {
        var command = CreateValidCommand() with
        {
            ExpiryMonth = month,
            ExpiryYear = DateTime.UtcNow.Year + 1
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task Validate_WithInvalidExpiryMonth_Fails(int month)
    {
        var command = CreateValidCommand() with { ExpiryMonth = month };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiryMonth");
    }

    #endregion

    #region Expiry Year

    [Fact]
    public async Task Validate_WithCurrentYear_Passes()
    {
        var now = DateTime.UtcNow;
        var command = CreateValidCommand() with
        {
            ExpiryMonth = now.Month,
            ExpiryYear = now.Year
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithFutureYear_Passes()
    {
        var command = CreateValidCommand() with { ExpiryYear = DateTime.UtcNow.Year + 5 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithPastYear_Fails()
    {
        var command = CreateValidCommand() with { ExpiryYear = DateTime.UtcNow.Year - 1 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiryYear");
    }

    #endregion

    #region Expiry Date (Composite)

    [Fact]
    public async Task Validate_WithCurrentYearAndPastMonth_Fails()
    {
        var now = DateTime.UtcNow;

        // Only testable when we're not in January
        if (now.Month == 1)
            return;

        var command = CreateValidCommand() with
        {
            ExpiryMonth = now.Month - 1,
            ExpiryYear = now.Year
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "ExpiryDate" &&
            e.ErrorMessage.Contains("expired"));
    }

    [Fact]
    public async Task Validate_WithFutureYearAndAnyMonth_Passes()
    {
        var command = CreateValidCommand() with
        {
            ExpiryMonth = 1,
            ExpiryYear = DateTime.UtcNow.Year + 1
        };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Currency

    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("EUR")]
    public async Task Validate_WithSupportedCurrency_Passes(string currency)
    {
        var command = CreateValidCommand() with { Currency = currency };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("gbp")]
    [InlineData("Eur")]
    public async Task Validate_WithLowercaseSupportedCurrency_Passes(string currency)
    {
        var command = CreateValidCommand() with { Currency = currency };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("JPY")]
    [InlineData("AUD")]
    [InlineData("XXX")]
    public async Task Validate_WithUnsupportedCurrency_Fails(string currency)
    {
        var command = CreateValidCommand() with { Currency = currency };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyOrNullCurrency_Fails(string? currency)
    {
        var command = CreateValidCommand() with { Currency = currency! };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public async Task Validate_WithWrongLengthCurrency_Fails(string currency)
    {
        var command = CreateValidCommand() with { Currency = currency };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    #endregion

    #region Amount

    [Fact]
    public async Task Validate_WithPositiveAmount_Passes()
    {
        var command = CreateValidCommand() with { Amount = 100 };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Validate_WithZeroOrNegativeAmount_Fails(int amount)
    {
        var command = CreateValidCommand() with { Amount = amount };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    #endregion

    #region CVV

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public async Task Validate_WithValidCvv_Passes(string cvv)
    {
        var command = CreateValidCommand() with { Cvv = cvv };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    public async Task Validate_WithInvalidLengthCvv_Fails(string cvv)
    {
        var command = CreateValidCommand() with { Cvv = cvv };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cvv");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12a")]
    public async Task Validate_WithNonNumericCvv_Fails(string cvv)
    {
        var command = CreateValidCommand() with { Cvv = cvv };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Cvv" &&
            e.ErrorMessage.Contains("numeric"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_WithEmptyOrNullCvv_Fails(string? cvv)
    {
        var command = CreateValidCommand() with { Cvv = cvv! };

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cvv");
    }

    #endregion
}