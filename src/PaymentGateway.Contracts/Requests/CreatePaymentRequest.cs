namespace PaymentGateway.Contracts.Requests;

/// <summary>
/// Request to create a new payment through the payment gateway.
/// </summary>
public record CreatePaymentRequest
{
    /// <summary>
    /// The card number (14-19 numeric characters).
    /// </summary>
    public required string CardNumber { get; init; }

    /// <summary>
    /// The card expiry month (1-12).
    /// </summary>
    public required int ExpiryMonth { get; init; }

    /// <summary>
    /// The card expiry year (must be current year or later).
    /// </summary>
    public required int ExpiryYear { get; init; }

    /// <summary>
    /// The card CVV (3-4 numeric characters).
    /// </summary>
    public required string Cvv { get; init; }

    /// <summary>
    /// ISO  currency code (e.g., "USD", "GBP", "EUR").
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// The payment amount in minor currency units.
    /// </summary>
    public required int Amount { get; init; }
}