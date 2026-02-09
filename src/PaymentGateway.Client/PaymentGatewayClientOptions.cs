namespace PaymentGateway.Client;

public sealed class PaymentGatewayClientOptions
{
    public const string SectionName = "PaymentGateway";
    public string BaseUrl { get; set; } = "https://localhost:7092";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
