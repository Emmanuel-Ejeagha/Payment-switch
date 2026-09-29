using System.Diagnostics.Metrics;
using System.Threading;

namespace BuildingBlocks.Shared.Observability;

/// <summary>
/// Prometheus signals for the transactional outbox (TASK-029): a backlog gauge
/// (messages awaiting publication) plus publish/failure counters. Each service's
/// outbox publisher feeds these so operators can alert on outbox lag.
/// </summary>
public static class OutboxMetrics
{
    private static readonly Meter Meter = new("PaymentSwitch", "1.0.0");
    private static readonly Counter<long> PublishedCounter =
        Meter.CreateCounter<long>("outbox_published_total", "messages", "Outbox messages published.");
    private static readonly Counter<long> FailedCounter =
        Meter.CreateCounter<long>("outbox_failed_total", "messages", "Outbox messages that failed to publish.");

    // The gauge has no live source of truth; publishers set it on every poll.
    private static long _backlog;

    static OutboxMetrics()
    {
        Meter.CreateObservableGauge(
            "outbox_backlog",
            () => Volatile.Read(ref _backlog),
            description: "Outbox messages awaiting publication in the last poll.");
    }

    public static void SetBacklog(long count) => Volatile.Write(ref _backlog, count);

    public static void RecordPublished(int count = 1) => PublishedCounter.Add(count);

    public static void RecordFailed(int count = 1) => FailedCounter.Add(count);
}
