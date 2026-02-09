using PaymentGateway.Application.Common;

namespace PaymentGateway.Application.BankClient;

public interface IBankClient
{
    Task<Result<BankResponse, PaymentRejected>> AuthorizePaymentAsync(BankRequest request, CancellationToken cancellationToken = default);
}