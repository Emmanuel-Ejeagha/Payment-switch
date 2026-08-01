using Grpc.Core;

namespace BuildingBlocks.Shared.Resilience;

public class GrpcResilienceOptions
{
    public int MaxRetryAttempts { get; set; } = 3;

    public TimeSpan PerAttemptTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(2);

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public TimeSpan CircuitBreakerOpenDuration { get; set; } = TimeSpan.FromSeconds(30);

    public IReadOnlyCollection<StatusCode> RetryableStatusCodes { get; set; } =
    [
        StatusCode.Unavailable,
        StatusCode.DeadlineExceeded,
        StatusCode.Aborted,
        StatusCode.ResourceExhausted,
    ];

    public bool IsRetryable(StatusCode statusCode)
    {
        return RetryableStatusCodes.Contains(statusCode);
    }

    public TimeSpan GetBackoffDelay(int attempt)
    {
        var backoff = BaseDelay * Math.Pow(2, attempt);
        var jitterMs = Random.Shared.NextDouble() * BaseDelay.TotalMilliseconds;
        backoff += TimeSpan.FromMilliseconds(jitterMs);
        return TimeSpan.FromTicks(Math.Min(backoff.Ticks, MaxDelay.Ticks));
    }
}
