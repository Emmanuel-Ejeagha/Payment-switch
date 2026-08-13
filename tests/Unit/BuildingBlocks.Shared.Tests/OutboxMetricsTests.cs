using System.Diagnostics.Metrics;
using BuildingBlocks.Shared.Observability;

namespace BuildingBlocks.Shared.Tests;

public class OutboxMetricsTests : IDisposable
{
    private static readonly object Gate = new();
    private static readonly List<(string Instrument, long Value)> Measurements = new();
    private static readonly MeterListener Listener;

    static OutboxMetricsTests()
    {
        Listener = new MeterListener();
        Listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "PaymentSwitch" && instrument.Name.StartsWith("outbox_", StringComparison.Ordinal))
                listener.EnableMeasurementEvents(instrument);
        };
        Listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            lock (Gate)
                Measurements.Add((instrument.Name, measurement));
        });
        Listener.Start();
    }

    public OutboxMetricsTests()
    {
        lock (Gate)
            Measurements.Clear();
    }

    public void Dispose()
    {
        // Listener is static and intentionally outlives individual tests so the
        // PaymentSwitch meter's static instruments remain enabled across them.
    }

    [Fact]
    public void SetBacklog_IsReportedByTheBacklogGauge()
    {
        OutboxMetrics.SetBacklog(7);
        Listener.RecordObservableInstruments();

        Assert.Contains(Measurements, m => m.Instrument == "outbox_backlog" && m.Value == 7);
    }

    [Fact]
    public void RecordPublishedAndFailed_ReportCounterIncrements()
    {
        OutboxMetrics.RecordPublished(3);
        OutboxMetrics.RecordFailed(2);

        Assert.Contains(Measurements, m => m.Instrument == "outbox_published_total" && m.Value == 3);
        Assert.Contains(Measurements, m => m.Instrument == "outbox_failed_total" && m.Value == 2);
    }
}
