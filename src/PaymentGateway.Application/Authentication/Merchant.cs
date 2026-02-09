namespace PaymentGateway.Application.Authentication;

public sealed record Merchant
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
