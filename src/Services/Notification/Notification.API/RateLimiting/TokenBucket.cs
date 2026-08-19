namespace Notification.API.RateLimiting;

/// <summary>
/// Fixed-capacity token bucket used to rate-limit SignalR hub invocations per
/// connection. Thread-safe; time is injectable for deterministic tests.
/// </summary>
public sealed class TokenBucket
{
    private readonly int _capacity;
    private readonly double _refillTokensPerSecond;
    private readonly object _gate = new();
    private double _tokens;
    private DateTimeOffset _lastRefill;

    public TokenBucket(int capacity, double refillTokensPerSecond, DateTimeOffset? now = null)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (refillTokensPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(refillTokensPerSecond));

        _capacity = capacity;
        _refillTokensPerSecond = refillTokensPerSecond;
        _tokens = capacity;
        _lastRefill = now ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Consumes <paramref name="tokens"/> if available and returns true; otherwise
    /// returns false without consuming. Refill is applied lazily before checking.
    /// </summary>
    public bool TryConsume(int tokens = 1, DateTimeOffset? now = null)
    {
        var current = now ?? DateTimeOffset.UtcNow;

        lock (_gate)
        {
            Refill(current);
            if (_tokens < tokens)
            {
                return false;
            }

            _tokens -= tokens;
            return true;
        }
    }

    private void Refill(DateTimeOffset now)
    {
        var elapsedSeconds = (now - _lastRefill).TotalSeconds;
        if (elapsedSeconds <= 0)
        {
            return;
        }

        _tokens = Math.Min(_capacity, _tokens + elapsedSeconds * _refillTokensPerSecond);
        _lastRefill = now;
    }
}