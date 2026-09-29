namespace Notification.API.RateLimiting;

/// <summary>
/// Configuration for SignalR hub invocation rate limiting. Bound from the
/// <c>SignalR:RateLimit</c> configuration section.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "SignalR:RateLimit";

    /// <summary>Maximum burst of invocations a connection may send.</summary>
    public int Capacity { get; set; } = 10;

    /// <summary>Steady-state refill rate in invocations per second.</summary>
    public double TokensPerSecond { get; set; } = 5;
}