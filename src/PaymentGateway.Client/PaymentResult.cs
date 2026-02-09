using PaymentGateway.Contracts.Responses;

namespace PaymentGateway.Client;

public sealed class PaymentResult
{
    public bool IsSuccess { get; init; }
    public CreatePaymentResponse? Payment { get; init; }
    public PaymentError? Error { get; init; }
    public bool WasIdempotentReplay { get; init; }

    public static PaymentResult Success(CreatePaymentResponse payment, bool wasReplay = false)
        => new() { IsSuccess = true, Payment = payment, WasIdempotentReplay = wasReplay };

    public static PaymentResult Failure(PaymentError error)
        => new() { IsSuccess = false, Error = error };
}