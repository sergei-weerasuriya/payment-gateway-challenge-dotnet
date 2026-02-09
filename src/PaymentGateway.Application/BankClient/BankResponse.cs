using System.Text.Json.Serialization;

namespace PaymentGateway.Application.BankClient;

public sealed record BankResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; init; }

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; init; }
}
