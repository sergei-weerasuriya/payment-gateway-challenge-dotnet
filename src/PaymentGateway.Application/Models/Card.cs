namespace PaymentGateway.Application.Models;

public sealed record Card
{
    public required string Number { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required string Cvv { get; init; }

    public string GetLastFourDigits()
    {
        if (string.IsNullOrEmpty(Number) || Number.Length < 4)
        {
            return string.Empty;
        }

        return Number[^4..];
    }

    public string GetMaskedNumber()
    {
        if (string.IsNullOrEmpty(Number) || Number.Length < 4)
        {
            return string.Empty;
        }

        var maskedLength = Number.Length - 4;
        return new string('*', maskedLength) + Number[^4..];
    }
}