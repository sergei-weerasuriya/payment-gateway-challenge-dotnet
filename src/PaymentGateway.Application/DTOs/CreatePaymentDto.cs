using PaymentGateway.Contracts.Models;

namespace PaymentGateway.Application.DTOs;

public record CreatePaymentDto
{
    public  Guid Id { get; init; }
    public PaymentStatus Status { get; init; }
    public string CardNumber { get; init; }
    public string CardLastFour { get; init; }
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public string Cvv { get; init; }
    public string Currency { get; init; }
    public int Amount { get; init; }
    public string? AuthorizationCode { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Guid MerchantId { get; init; }
}