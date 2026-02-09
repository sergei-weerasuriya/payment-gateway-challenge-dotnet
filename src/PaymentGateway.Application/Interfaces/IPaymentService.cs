using PaymentGateway.Application.Common;
using PaymentGateway.Application.DTOs;

namespace PaymentGateway.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentResult, PaymentRejected>> ProcessPaymentAsync(ProcessPaymentCommand command, Guid merchantId, CancellationToken cancellationToken = default);
    Task<PaymentResult?> GetPaymentAsync(Guid paymentId, Guid merchantId, CancellationToken cancellationToken = default);
}
