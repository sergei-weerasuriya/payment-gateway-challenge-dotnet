using System.Diagnostics;

namespace PaymentGateway.Application.Metrics;

public class PaymentTimer : IDisposable
{
    private readonly PaymentMetrics _metrics;
    private readonly string _currency;
    private readonly Stopwatch _stopwatch;
    private string _status = "unknown";

    internal PaymentTimer(PaymentMetrics metrics, string currency)
    {
        _metrics = metrics;
        _currency = currency;
        _stopwatch = Stopwatch.StartNew();
    }

    public void SetStatus(string status)
    {
        _status = status;
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _metrics.RecordPaymentDuration(_stopwatch.Elapsed.TotalMilliseconds, _currency, _status);
    }
}