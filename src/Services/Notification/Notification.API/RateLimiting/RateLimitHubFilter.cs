using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Notification.API.RateLimiting;

/// <summary>
/// Per-connection token bucket applied to every client-to-server hub invocation
/// (TASK-047). A connection that exhausts its budget receives a HubException;
/// buckets are released when the connection disconnects so memory is bounded.
/// </summary>
public sealed class RateLimitHubFilter : IHubFilter
{
    private readonly RateLimitOptions _options;
    private readonly ConcurrentDictionary<string, BucketEntry> _buckets = new();

    public RateLimitHubFilter(IOptions<RateLimitOptions> options)
    {
        _options = options.Value;
    }

    public ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext context,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var bucket = _buckets.GetOrAdd(
            context.Context.ConnectionId,
            static (_, options) => new BucketEntry(options, DateTimeOffset.UtcNow),
            _options);

        if (!bucket.TryConsume(DateTimeOffset.UtcNow))
        {
            throw new HubException("Rate limit exceeded. Slow down and retry.");
        }

        return next(context);
    }

    public ValueTask OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, ValueTask> next)
    {
        _buckets.TryRemove(context.Context.ConnectionId, out _);
        return next(context, exception);
    }

    private sealed class BucketEntry
    {
        private readonly TokenBucket _bucket;

        public BucketEntry(RateLimitOptions options, DateTimeOffset now)
        {
            _bucket = new TokenBucket(options.Capacity, options.TokensPerSecond, now);
        }

        public bool TryConsume(DateTimeOffset now) => _bucket.TryConsume(1, now);
    }
}