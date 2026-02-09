using PaymentGateway.Contracts.Models;

namespace PaymentGateway.Application.DTOs;

public sealed record PaymentDto
{
    public required Guid Id { get; init; }
    public required PaymentStatus Status { get; init; }
    public required string CardNumber { get; init; }
    public required string CardLastFour { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required string Cvv { get; init; }
    public required string Currency { get; init; }
    public required int Amount { get; init; }
    public string? AuthorizationCode { get; init; }
    public DateTime CreatedAt { get; init; }
    public required Guid MerchantId { get; init; }
}
