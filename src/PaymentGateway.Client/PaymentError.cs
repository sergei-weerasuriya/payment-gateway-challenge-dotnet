namespace PaymentGateway.Client;

public sealed class PaymentError
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
}