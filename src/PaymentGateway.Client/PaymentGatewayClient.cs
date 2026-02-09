using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentGateway.Contracts.Requests;
using PaymentGateway.Contracts.Responses;

namespace PaymentGateway.Client;

public sealed class PaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaymentGatewayClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<PaymentResult> ProcessPaymentAsync(CreatePaymentRequest request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var wasReplay = response.Headers.Contains("X-Idempotent-Replay");

        if (response.IsSuccessStatusCode)
        {
            var payment = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>(JsonOptions, cancellationToken);
            return PaymentResult.Success(payment!, wasReplay);
        }

        return await HandleErrorResponseAsync(response, cancellationToken);
    }


    public async Task<GetPaymentResponse?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/payments/{paymentId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions, cancellationToken);
    }

    private static async Task<PaymentResult> HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(content, JsonOptions);
            if (problemDetails is not null && (problemDetails.Title is not null || problemDetails.Detail is not null))
            {
                return PaymentResult.Failure(new PaymentError
                {
                    StatusCode = statusCode,
                    Message = problemDetails.Detail ?? problemDetails.Title ?? "Request failed",
                    ValidationErrors = problemDetails.Errors
                });
            }
        }
        catch (JsonException)
        {
        }

        return PaymentResult.Failure(new PaymentError
        {
            StatusCode = statusCode,
            Message = string.IsNullOrEmpty(content) ? "Request failed" : content
        });
    }
}