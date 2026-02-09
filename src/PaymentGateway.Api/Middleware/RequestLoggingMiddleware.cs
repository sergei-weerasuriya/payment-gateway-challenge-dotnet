using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace PaymentGateway.Api.Middleware;

public sealed partial class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    private static readonly Regex CardNumberPattern = CardNumberRegex();
    private static readonly Regex CvvPattern = CvvRegex();

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.ToString(),
            ["RequestMethod"] = context.Request.Method
        });

        var stopwatch = Stopwatch.StartNew();

        await LogRequestAsync(context, correlationId);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        context.Request.EnableBuffering();

        var requestBody = string.Empty;
        if (context.Request.ContentLength > 0 && context.Request.ContentLength < 10000)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            requestBody = MaskSensitiveData(requestBody);
        }

        _logger.LogInformation(
            "HTTP {Method} {Path} started | CorrelationId: {CorrelationId} | Body: {Body}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            string.IsNullOrEmpty(requestBody) ? "(empty)" : requestBody);
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMs)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        responseBody = MaskSensitiveData(responseBody);

        var logLevel = context.Response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

        _logger.Log(
            logLevel,
            "HTTP {Method} {Path} completed | Status: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId} | Body: {Body}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMs,
            correlationId,
            string.IsNullOrEmpty(responseBody) ? "(empty)" : TruncateIfNeeded(responseBody, 1000));
    }

    private static string MaskSensitiveData(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        content = CardNumberPattern.Replace(content, match =>
        {
            var cardNumber = match.Groups[1].Value;
            if (cardNumber.Length >= 4)
            {
                var masked = new string('*', cardNumber.Length - 4) + cardNumber[^4..];
                return $"\"{match.Groups[0].Value.Split(':')[0].Trim('"')}\":\"{masked}\"";
            }
            return match.Value;
        });

        content = CvvPattern.Replace(content, "\"$1\":\"***\"");

        return content;
    }

    private static string TruncateIfNeeded(string content, int maxLength)
    {
        if (content.Length <= maxLength)
        {
            return content;
        }

        return content[..maxLength] + "... (truncated)";
    }

    [GeneratedRegex(@"""(card[_]?number|cardNumber)""\s*:\s*""(\d{14,19})""", RegexOptions.IgnoreCase)]
    private static partial Regex CardNumberRegex();

    [GeneratedRegex(@"""(cvv|cvc|securityCode)""\s*:\s*""\d{3,4}""", RegexOptions.IgnoreCase)]
    private static partial Regex CvvRegex();
}