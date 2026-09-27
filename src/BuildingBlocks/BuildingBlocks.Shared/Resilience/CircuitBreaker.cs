namespace BuildingBlocks.Shared.Resilience;

public class CircuitBreaker
{
    private readonly object _lock = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private int _consecutiveFailures;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTimeOffset _openedAt;
    private DateTimeOffset _lastProbeAt;

    public CircuitBreaker(int failureThreshold, TimeSpan openDuration)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration;
    }

    public bool CanProceed()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return true;
                case CircuitBreakerState.Open when DateTimeOffset.UtcNow - _openedAt >= _openDuration:
                    _state = CircuitBreakerState.HalfOpen;
                    _lastProbeAt = DateTimeOffset.UtcNow;
                    return true;
                case CircuitBreakerState.HalfOpen when DateTimeOffset.UtcNow - _lastProbeAt >= TimeSpan.FromMilliseconds(500):
                    return true;
                default:
                    return false;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            _state = CircuitBreakerState.Closed;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _consecutiveFailures++;
            if (_state == CircuitBreakerState.HalfOpen || _consecutiveFailures >= _failureThreshold)
            {
                _state = CircuitBreakerState.Open;
                _openedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen,
}
