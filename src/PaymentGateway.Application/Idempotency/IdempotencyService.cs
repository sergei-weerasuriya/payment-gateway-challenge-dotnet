using System.Collections.Concurrent;

namespace PaymentGateway.Application.Idempotency;

public class IdempotencyService : IIdempotencyService
{
    private readonly ConcurrentDictionary<string, IdempotentResponse> _responses = new();
    private readonly ConcurrentDictionary<string, (Guid Owner, DateTime AcquiredAt)> _locks = new();
    private readonly TimeSpan _responseExpiry = TimeSpan.FromHours(24);
    private readonly TimeSpan _lockExpiry = TimeSpan.FromMinutes(5);

    public Task<IdempotentResponse?> GetAsync(string idempotencyKey)
    {
        if (_responses.TryGetValue(idempotencyKey, out var response))
        {
            if (DateTime.UtcNow - response.CreatedAt < _responseExpiry)
            {
                return Task.FromResult<IdempotentResponse?>(response);
            }

            _responses.TryRemove(idempotencyKey, out _);
        }

        return Task.FromResult<IdempotentResponse?>(null);
    }

    public Task SetAsync(string idempotencyKey, IdempotentResponse response)
    {
        _responses[idempotencyKey] = response;
        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireLockAsync(string idempotencyKey)
    {
        var owner = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var result = _locks.AddOrUpdate(
            idempotencyKey,
            (owner, now),
            (key, existing) =>
            {
                if (now - existing.AcquiredAt > _lockExpiry)
                {
                    return (owner, now);
                }
                return existing;
            });

        return Task.FromResult(result.Owner == owner);
    }

    public Task ReleaseLockAsync(string idempotencyKey)
    {
        _locks.TryRemove(idempotencyKey, out _);
        return Task.CompletedTask;
    }

    public void CleanupExpired()
    {
        var now = DateTime.UtcNow;

        foreach (var key in _responses.Keys)
        {
            if (_responses.TryGetValue(key, out var response) &&
                now - response.CreatedAt >= _responseExpiry)
            {
                _responses.TryRemove(key, out _);
            }
        }

        foreach (var key in _locks.Keys)
        {
            if (_locks.TryGetValue(key, out var lockEntry) && now - lockEntry.AcquiredAt >= _lockExpiry)
            {
                _locks.TryRemove(key, out _);
            }
        }
    }
}