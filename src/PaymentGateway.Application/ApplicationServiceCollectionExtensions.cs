using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Application.Authentication;
using PaymentGateway.Application.BankClient;
using PaymentGateway.Application.Encryption;
using PaymentGateway.Application.Idempotency;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Metrics;
using PaymentGateway.Application.Repositories;
using PaymentGateway.Application.Services;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Validation;

namespace PaymentGateway.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<IValidator<ProcessPaymentCommand>, ProcessPaymentCommandValidator>();
        services.AddSingleton<IPaymentRepository, PaymentRepository>();
        services.AddSingleton<IMerchantRepository, MerchantRepository>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddSingleton<IIdempotencyService, IdempotencyService>();

        if (configuration is not null)
        {
            services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));
        }
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddSingleton<PaymentMetrics>();

        var bankClientOptions = new BankClientOptions();
        configuration?.GetSection(BankClientOptions.SectionName).Bind(bankClientOptions);

        services.AddHttpClient<IBankClient, BankClient.BankClient>(client =>
        {
            client.BaseAddress = new Uri(bankClientOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(bankClientOptions.TimeoutSeconds);
        });

        return services;
    }
}