using PaymentGateway.Contracts.Requests;
using PaymentGateway.Contracts.Responses;

namespace PaymentGateway.Client;

public interface IPaymentGatewayClient
{
    Task<PaymentResult> ProcessPaymentAsync(CreatePaymentRequest request, string idempotencyKey, CancellationToken cancellationToken = default);

    Task<GetPaymentResponse?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
}