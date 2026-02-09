namespace PaymentGateway.Application.Repositories;

internal sealed class StoredPayment
{
    public Guid Id { get; init; }
    public required Contracts.Models.PaymentStatus Status { get; init; }
    public required string EncryptedCardNumber { get; init; }
    public required string CardLastFour { get; init; }
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public required string EncryptedCvv { get; init; }
    public required string Currency { get; init; }
    public int Amount { get; init; }
    public string? AuthorizationCode { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid MerchantId { get; init; }
}