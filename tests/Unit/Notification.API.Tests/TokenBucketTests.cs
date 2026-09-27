using Notification.API.RateLimiting;

namespace Notification.API.Tests;

public class TokenBucketTests
{
    [Fact]
    public void StartsFull_AllowsFullBurst()
    {
        var bucket = new TokenBucket(capacity: 10, refillTokensPerSecond: 5, now: Start);

        for (var i = 0; i < 10; i++)
        {
            Assert.True(bucket.TryConsume(1, Start));
        }
    }

    [Fact]
    public void Exhausted_RejectsFurtherConsumption()
    {
        var bucket = new TokenBucket(capacity: 2, refillTokensPerSecond: 1, now: Start);
        Assert.True(bucket.TryConsume(2, Start));
        Assert.False(bucket.TryConsume(1, Start));
    }

    [Fact]
    public void RefillsOverTime()
    {
        var bucket = new TokenBucket(capacity: 2, refillTokensPerSecond: 1, now: Start);
        Assert.True(bucket.TryConsume(2, Start));
        Assert.False(bucket.TryConsume(1, Start));

        var afterOneSecond = Start.AddSeconds(1);
        Assert.True(bucket.TryConsume(1, afterOneSecond));
    }

    [Fact]
    public void RefillIsCappedAtCapacity()
    {
        var bucket = new TokenBucket(capacity: 2, refillTokensPerSecond: 1, now: Start);
        Assert.True(bucket.TryConsume(1, Start));

        var muchLater = Start.AddHours(1);
        Assert.True(bucket.TryConsume(2, muchLater));
        Assert.False(bucket.TryConsume(1, muchLater));
    }

    [Fact]
    public void PartialRefill_IsNotEnough_ForFullToken()
    {
        var bucket = new TokenBucket(capacity: 2, refillTokensPerSecond: 1, now: Start);
        Assert.True(bucket.TryConsume(2, Start));

        Assert.False(bucket.TryConsume(1, Start.AddMilliseconds(499)));
        Assert.True(bucket.TryConsume(1, Start.AddSeconds(1)));
    }

    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}