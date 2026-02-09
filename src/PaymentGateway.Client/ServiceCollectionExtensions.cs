using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentGatewayClient(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new PaymentGatewayClientOptions();
        configuration.GetSection(PaymentGatewayClientOptions.SectionName).Bind(options);

        return services.AddPaymentGatewayClient(options);
    }

    public static IServiceCollection AddPaymentGatewayClient(this IServiceCollection services, PaymentGatewayClientOptions options)
    {
        services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }
        });

        return services;
    }

    public static IServiceCollection AddPaymentGatewayClient(this IServiceCollection services, string baseUrl)
    {
        return services.AddPaymentGatewayClient(new PaymentGatewayClientOptions
        {
            BaseUrl = baseUrl
        });
    }
}