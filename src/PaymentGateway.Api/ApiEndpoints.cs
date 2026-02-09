namespace PaymentGateway.Api;

public static class ApiEndpoints
{
    private const string ApiBase = "api";

    public static class Payments
    {
        private const string Base = $"{ApiBase}/payments";
        public const string Create = Base;
        public const string Get = $"{Base}/{{id:guid}}";
    }
}