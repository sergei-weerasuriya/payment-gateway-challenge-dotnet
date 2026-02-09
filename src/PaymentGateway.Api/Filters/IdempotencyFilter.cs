using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using PaymentGateway.Application.Idempotency;

namespace PaymentGateway.Api.Filters;

public sealed class IdempotencyFilter : IAsyncActionFilter
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    private const int MaxKeyLength = 64;

    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<IdempotencyFilter> _logger;

    public IdempotencyFilter(IIdempotencyService idempotencyService, ILogger<IdempotencyFilter> logger)
    {
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Missing Idempotency Key",
                Detail = $"The '{IdempotencyKeyHeader}' header is required for POST requests.",
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        var idempotencyKey = keyValues.First()!;

        if (idempotencyKey.Length > MaxKeyLength)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = "Invalid Idempotency Key",
                Detail = $"The '{IdempotencyKeyHeader}' must be {MaxKeyLength} characters or less.",
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        _logger.LogInformation("Processing request with idempotency key: {IdempotencyKey}", idempotencyKey);

        var cachedResponse = await _idempotencyService.GetAsync(idempotencyKey);
        if (cachedResponse is not null)
        {
            _logger.LogInformation("Returning cached response for idempotency key: {IdempotencyKey}", idempotencyKey);

            context.HttpContext.Response.Headers["X-Idempotent-Replay"] = "true";

            if (cachedResponse.Headers is not null)
            {
                foreach (var header in cachedResponse.Headers)
                {
                    context.HttpContext.Response.Headers[header.Key] = header.Value;
                }
            }

            context.Result = new ContentResult
            {
                StatusCode = cachedResponse.StatusCode,
                Content = cachedResponse.Body,
                ContentType = cachedResponse.ContentType
            };
            return;
        }

        if (!await _idempotencyService.TryAcquireLockAsync(idempotencyKey))
        {
            _logger.LogWarning("Concurrent request detected for idempotency key: {IdempotencyKey}", idempotencyKey);

            context.Result = new ConflictObjectResult(new ProblemDetails
            {
                Title = "Duplicate Request In Progress",
                Detail = "A request with this idempotency key is already being processed. Please retry later.",
                Status = StatusCodes.Status409Conflict
            });
            return;
        }

        try
        {
            var executedContext = await next();

            if (executedContext.Result is ObjectResult objectResult)
            {
                var responseBody = JsonSerializer.Serialize(objectResult.Value);
                var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;

                Dictionary<string, string>? headers = null;
                if (executedContext.Result is CreatedAtActionResult createdAtResult)
                {
                    var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
                    var urlHelper = urlHelperFactory.GetUrlHelper(context);
                    var request = context.HttpContext.Request;
                    var locationUrl = urlHelper.Action(
                        createdAtResult.ActionName,
                        createdAtResult.ControllerName,
                        createdAtResult.RouteValues,
                        request.Scheme,
                        request.Host.ToUriComponent());
                    if (locationUrl is not null)
                    {
                        headers = new Dictionary<string, string> { { "Location", locationUrl } };
                    }
                }

                await _idempotencyService.SetAsync(idempotencyKey, new IdempotentResponse
                {
                    StatusCode = statusCode,
                    Body = responseBody,
                    ContentType = "application/json",
                    Headers = headers
                });

                _logger.LogInformation("Stored response for idempotency key: {IdempotencyKey}, StatusCode: {StatusCode}", idempotencyKey, statusCode);
            }
        }
        finally
        {
            await _idempotencyService.ReleaseLockAsync(idempotencyKey);
        }
    }
}