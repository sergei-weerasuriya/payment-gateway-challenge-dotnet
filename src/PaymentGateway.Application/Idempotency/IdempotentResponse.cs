namespace PaymentGateway.Application.Idempotency;

public sealed record IdempotentResponse
{
    public required int StatusCode { get; init; }
    public required string Body { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string ContentType { get; init; } = "application/json";
}