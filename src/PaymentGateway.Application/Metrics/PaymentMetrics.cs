using System.Diagnostics.Metrics;

namespace PaymentGateway.Application.Metrics;

public class PaymentMetrics : IDisposable
{
    public const string MeterName = "PaymentGateway";

    private readonly Meter _meter;
    private readonly Counter<long> _paymentsProcessed;
    private readonly Counter<long> _paymentsAuthorized;
    private readonly Counter<long> _paymentsDeclined;
    private readonly Counter<long> _paymentsRejected;
    private readonly Counter<long> _bankErrors;
    private readonly Histogram<double> _paymentDuration;
    private readonly Histogram<double> _bankLatency;
    private readonly Counter<long> _idempotentReplays;

    public PaymentMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _paymentsProcessed = _meter.CreateCounter<long>(
            "payments_processed_total",
            unit: "payments",
            description: "Total number of payments processed");

        _paymentsAuthorized = _meter.CreateCounter<long>(
            "payments_authorized_total",
            unit: "payments",
            description: "Total number of authorized payments");

        _paymentsDeclined = _meter.CreateCounter<long>(
            "payments_declined_total",
            unit: "payments",
            description: "Total number of declined payments");

        _paymentsRejected = _meter.CreateCounter<long>(
            "payments_rejected_total",
            unit: "payments",
            description: "Total number of rejected payments (validation failures)");

        _bankErrors = _meter.CreateCounter<long>(
            "bank_errors_total",
            unit: "errors",
            description: "Total number of bank communication errors");

        _paymentDuration = _meter.CreateHistogram<double>(
            "payment_processing_duration_ms",
            unit: "ms",
            description: "Duration of payment processing in milliseconds");

        _bankLatency = _meter.CreateHistogram<double>(
            "bank_request_latency_ms",
            unit: "ms",
            description: "Latency of bank API requests in milliseconds");

        _idempotentReplays = _meter.CreateCounter<long>(
            "idempotent_replays_total",
            unit: "replays",
            description: "Total number of idempotent request replays");
    }

    public void RecordPaymentProcessed(string currency)
    {
        _paymentsProcessed.Add(1, new KeyValuePair<string, object?>("currency", currency));
    }

    public void RecordPaymentAuthorized(string currency)
    {
        _paymentsAuthorized.Add(1, new KeyValuePair<string, object?>("currency", currency));
    }

    public void RecordPaymentDeclined(string currency)
    {
        _paymentsDeclined.Add(1, new KeyValuePair<string, object?>("currency", currency));
    }

    public void RecordPaymentRejected()
    {
        _paymentsRejected.Add(1);
    }

    public void RecordBankError()
    {
        _bankErrors.Add(1);
    }

    public void RecordPaymentDuration(double milliseconds, string currency, string status)
    {
        _paymentDuration.Record(milliseconds, new KeyValuePair<string, object?>("currency", currency), new KeyValuePair<string, object?>("status", status));
    }

    public void RecordBankLatency(double milliseconds, bool success)
    {
        _bankLatency.Record(milliseconds, new KeyValuePair<string, object?>("success", success));
    }

    public void RecordIdempotentReplay()
    {
        _idempotentReplays.Add(1);
    }

    public PaymentTimer StartPaymentTimer(string currency)
    {
        return new PaymentTimer(this, currency);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}