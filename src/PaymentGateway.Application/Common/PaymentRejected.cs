namespace PaymentGateway.Application.Common;

public sealed class PaymentRejected
{
    public Dictionary<string, string[]> Errors { get; }
    public string Message { get; }

    public PaymentRejected(string message, Dictionary<string, string[]>? errors = null)
    {
        Message = message;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public static PaymentRejected FromFieldError(string fieldName, string errorMessage)
    {
        return new PaymentRejected(
            "The request was rejected due to validation errors.",
            new Dictionary<string, string[]>
            {
                { fieldName, new[] { errorMessage } }
            });
    }

    public static PaymentRejected FromFieldErrors(Dictionary<string, string[]> errors)
    {
        return new PaymentRejected("The request was rejected due to validation errors.", errors);
    }

    public static PaymentRejected BankUnavailable()
    {
        return new PaymentRejected("Unable to process payment. The acquiring bank is currently unavailable.");
    }
}