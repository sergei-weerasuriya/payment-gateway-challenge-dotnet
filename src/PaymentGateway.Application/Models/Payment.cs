using PaymentGateway.Contracts.Models;

namespace PaymentGateway.Application.Models;

public sealed class Payment
{
    public required Guid Id { get; init; }
    public required PaymentStatus Status { get; set; }
    public required Card Card { get; init; }
    public required string Currency { get; init; }
    public required int Amount { get; init; }
    public string? AuthorizationCode { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public required Guid MerchantId { get; init; }
}
