using BuildingBlocks.Shared.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;

namespace BuildingBlocks.Shared.Tests;

public class RateLimitPartitionKeyTests
{
    private static DefaultHttpContext AnonymousContext(string? ip)
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = ip is null ? null : IPAddress.Parse(ip);
        return ctx;
    }

    private static DefaultHttpContext UserContext(string userId, string? ip = null)
    {
        var ctx = AnonymousContext(ip);
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        return ctx;
    }

    [Fact]
    public void GetKey_AuthenticatedUser_UsesUserId()
    {
        Assert.Equal("user:u-1", RateLimitPartitionKey.GetKey(UserContext("u-1", "10.0.0.1")));
    }

    [Fact]
    public void GetKey_Anonymous_UsesIp()
    {
        Assert.Equal("ip:10.0.0.7", RateLimitPartitionKey.GetKey(AnonymousContext("10.0.0.7")));
    }

    [Fact]
    public void GetKey_AnonymousWithoutIp_UsesUnknown()
    {
        Assert.Equal("ip:unknown", RateLimitPartitionKey.GetKey(AnonymousContext(null)));
    }

    [Fact]
    public void GetKey_DistinctIps_DistinctKeys()
    {
        Assert.NotEqual(
            RateLimitPartitionKey.GetKey(AnonymousContext("10.0.0.1")),
            RateLimitPartitionKey.GetKey(AnonymousContext("10.0.0.2")));
    }

    [Fact]
    public void GetKey_AuthenticatedUserBeatsIp()
    {
        Assert.Equal(
            RateLimitPartitionKey.GetKey(UserContext("u-9", "10.0.0.1")),
            RateLimitPartitionKey.GetKey(UserContext("u-9", "10.0.0.2")));
    }
}

/// <summary>
/// Step 7.4: the global limiter must be per-client, not one global bucket.
/// Exhausting IP-A's window must reject IP-A while IP-B is still admitted.
/// </summary>
public class PartitionedGlobalLimiterTests
{
    private static DefaultHttpContext ContextFor(string ip)
    {
        var ctx = new DefaultHttpContext();
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return ctx;
    }

    [Fact]
    public void GlobalLimiter_ExhaustedPartition_RejectsSameIpButAdmitsOtherIp()
    {
        var services = new ServiceCollection();
        services.AddPaymentSwitchRateLimiting();
        using var provider = services.BuildServiceProvider();
        var limiter = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter!;
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        Assert.Equal(StatusCodes.Status429TooManyRequests, options.RejectionStatusCode);

        var ipA = ContextFor("10.10.0.1");
        var ipB = ContextFor("10.10.0.2");

        for (var i = 0; i < 100; i++)
            Assert.True(limiter.AttemptAcquire(ipA).IsAcquired);

        Assert.False(limiter.AttemptAcquire(ipA).IsAcquired);
        Assert.True(limiter.AttemptAcquire(ipB).IsAcquired);
    }
}
