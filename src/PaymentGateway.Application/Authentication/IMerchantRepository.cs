namespace PaymentGateway.Application.Authentication;

public interface IMerchantRepository
{
    Task<Merchant?> GetByApiKeyAsync(string apiKey);
}
