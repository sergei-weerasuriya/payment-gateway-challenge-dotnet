using FluentValidation;

using PaymentGateway.Application.DTOs;

namespace PaymentGateway.Application.Validation;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    private static readonly HashSet<string> _SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD",
        "GBP",
        "EUR"
    };

    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .WithMessage("Card number is required.")
            .Length(14, 19)
            .WithMessage("Card number must be between 14 and 19 characters.")
            .Must(BeNumericOnly)
            .WithMessage("Card number must contain only numeric characters.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("Expiry month must be between 1 and 12.");

        RuleFor(x => x.ExpiryYear)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Year)
            .WithMessage("Expiry year must not be in the past.");

        RuleFor(x => x)
            .Must(HaveValidExpiryDate)
            .WithName("ExpiryDate")
            .WithMessage("Card has expired. Expiry date must be in the future.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be exactly 3 characters.")
            .Must(BeSupportedCurrency)
            .WithMessage($"Currency must be one of: {string.Join(", ", _SupportedCurrencies)}.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .WithMessage("CVV is required.")
            .Length(3, 4)
            .WithMessage("CVV must be 3 or 4 characters.")
            .Must(BeNumericOnly)
            .WithMessage("CVV must contain only numeric characters.");
    }

    private static bool BeNumericOnly(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.All(char.IsDigit);
    }

    private static bool BeSupportedCurrency(string? currency)
    {
        if (string.IsNullOrEmpty(currency))
        {
            return false;
        }

        return _SupportedCurrencies.Contains(currency);
    }

    private static bool HaveValidExpiryDate(ProcessPaymentCommand command)
    {
        var now = DateTime.UtcNow;
        var currentYear = now.Year;
        var currentMonth = now.Month;

        if (command.ExpiryYear > currentYear)
        {
            return true;
        }

        if (command.ExpiryYear == currentYear)
        {
            return command.ExpiryMonth >= currentMonth;
        }

        return false;
    }
}
