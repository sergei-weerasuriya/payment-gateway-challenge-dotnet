using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PaymentGateway.Application.Common;

namespace PaymentGateway.Application.BankClient;

public class BankClient : IBankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BankClient> _logger;

    public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<Result<BankResponse, PaymentRejected>> AuthorizePaymentAsync(BankRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending payment authorization request to bank for card ending in {CardLastFour}",
            request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "****");

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                _logger.LogWarning("Bank returned 503 Service Unavailable");
                return PaymentRejected.BankUnavailable();
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Bank returned unexpected status code: {StatusCode}", response.StatusCode);
                return PaymentRejected.BankUnavailable();
            }

            var bankResponse = await response.Content.ReadFromJsonAsync<BankResponse>(cancellationToken);

            if (bankResponse is null)
            {
                _logger.LogError("Failed to deserialize bank response");
                return PaymentRejected.BankUnavailable();
            }

            _logger.LogInformation("Bank authorization response: Authorized={Authorized}", bankResponse.Authorized);

            return bankResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to bank failed");
            return PaymentRejected.BankUnavailable();
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request to bank timed out");
            return PaymentRejected.BankUnavailable();
        }
    }
}