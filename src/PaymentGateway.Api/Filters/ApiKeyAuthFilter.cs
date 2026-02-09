using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PaymentGateway.Application.Authentication;

namespace PaymentGateway.Api.Filters;

public sealed class ApiKeyAuthFilter : IAsyncActionFilter
{
    public const string ApiKeyHeader = "X-Api-Key";
    public const string MerchantContextKey = "Merchant";

    private readonly IMerchantRepository _merchantRepository;
    private readonly ILogger<ApiKeyAuthFilter> _logger;

    public ApiKeyAuthFilter(IMerchantRepository merchantRepository, ILogger<ApiKeyAuthFilter> logger)
    {
        _merchantRepository = merchantRepository;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            _logger.LogWarning("Request missing {Header} header", ApiKeyHeader);

            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Missing API Key",
                Detail = $"The '{ApiKeyHeader}' header is required.",
                Status = StatusCodes.Status401Unauthorized
            });
            return;
        }

        var apiKey = keyValues.First();

        var merchant = await _merchantRepository.GetByApiKeyAsync(apiKey);
        if (merchant is null)
        {
            _logger.LogWarning("Invalid API key provided");

            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Title = "Invalid API Key",
                Detail = "The provided API key is not valid.",
                Status = StatusCodes.Status401Unauthorized
            });
            return;
        }

        _logger.LogInformation("Authenticated merchant {MerchantId} ({MerchantName})", merchant.Id, merchant.Name);

        context.HttpContext.Items[MerchantContextKey] = merchant;

        await next();
    }
}