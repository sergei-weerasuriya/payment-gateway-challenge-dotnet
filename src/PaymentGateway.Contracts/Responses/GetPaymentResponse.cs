using PaymentGateway.Contracts.Models;

namespace PaymentGateway.Contracts.Responses;

public record GetPaymentResponse
{
    public Guid Id { get; init; }

    /// <summary>
    /// Authorized or Declined.
    /// </summary>
    public PaymentStatus Status { get; init; }

    /// <summary>
    /// Last four digits of the card number.
    /// </summary>
    public string CardNumberLastFour { get; init; }

    /// <summary>
    /// Card expiry month (1-12).
    /// </summary>
    public int ExpiryMonth { get; init; }

    /// <summary>
    /// Card expiry year.
    /// </summary>
    public int ExpiryYear { get; init; }

    /// <summary>
    /// ISO  currency code (e.g., "USD", "GBP", "EUR").
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// Payment amount in minor currency units.
    /// </summary>
    public int Amount { get; init; }
}