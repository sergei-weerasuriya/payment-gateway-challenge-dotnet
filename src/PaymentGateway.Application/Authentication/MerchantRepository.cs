using System.Collections.Concurrent;

namespace PaymentGateway.Application.Authentication;

public class MerchantRepository : IMerchantRepository
{
    private readonly ConcurrentDictionary<string, Merchant> _merchants = new();

    public MerchantRepository()
    {
        SeedMerchant("merchant-key-1", Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "Amazon");
        SeedMerchant("merchant-key-2", Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"), "Apple");
    }

    public Task<Merchant?> GetByApiKeyAsync(string apiKey)
    {
        _merchants.TryGetValue(apiKey, out var merchant);
        return Task.FromResult(merchant);
    }

    private void SeedMerchant(string apiKey, Guid id, string name)
    {
        _merchants[apiKey] = new Merchant { Id = id, Name = name };
    }
}