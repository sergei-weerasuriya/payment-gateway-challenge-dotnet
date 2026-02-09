using PaymentGateway.Api.Filters;
using PaymentGateway.Application.Authentication;

namespace PaymentGateway.Api.Extensions;

public static class HttpContextExtensions
{
    public static Merchant GetMerchant(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(ApiKeyAuthFilter.MerchantContextKey, out var value) && value is Merchant merchant)
        {
            return merchant;
        }

        throw new InvalidOperationException("Merchant not found in HttpContext. Ensure the [ApiKeyAuth] attribute is applied.");
    }
}
