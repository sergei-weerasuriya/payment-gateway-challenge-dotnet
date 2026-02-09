namespace PaymentGateway.Application.Idempotency;

public interface IIdempotencyService
{
    Task<IdempotentResponse?> GetAsync(string idempotencyKey);
    Task SetAsync(string idempotencyKey, IdempotentResponse response);
    Task<bool> TryAcquireLockAsync(string idempotencyKey);
    Task ReleaseLockAsync(string idempotencyKey);
}