using System.Net.Http.Json;

namespace PaymentGateway.Api.Tests;

// Extension methods for HttpClient to simplify testing with idempotency and API key auth.
public static class HttpClientExtensions
{
    // Default test API key corresponding to the  "Amazon" merchant.
    public const string DefaultApiKey = "merchant-key-1";
    
    // Second test API key corresponding to the "Apple" merchant.
    public const string SecondMerchantApiKey = "merchant-key-2";
    
    // Sends a POST request with a random idempotency key and default API key.
    public static async Task<HttpResponseMessage> PostAsJsonWithIdempotencyAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken = default)
    {
        return await client.PostAsJsonWithIdempotencyAsync(requestUri, value, Guid.NewGuid().ToString(), cancellationToken);
    }
    
    // Sends a POST request with a specified idempotency key and default API key.
    public static async Task<HttpResponseMessage> PostAsJsonWithIdempotencyAsync<T>(this HttpClient client, string requestUri, T value, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(value)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.Add("X-Api-Key", DefaultApiKey);

        return await client.SendAsync(request, cancellationToken);
    }
    
    // Sends a GET request with the specified API key (defaults to the test merchant key).
    public static async Task<HttpResponseMessage> GetWithApiKeyAsync(this HttpClient client, string requestUri, string apiKey = DefaultApiKey, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-Api-Key", apiKey);
        return await client.SendAsync(request, cancellationToken);
    }
}