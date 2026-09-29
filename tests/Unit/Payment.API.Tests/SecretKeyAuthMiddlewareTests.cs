using BuildingBlocks.Shared.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Payment.API.Configuration;
using Payment.API.Middlewares;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using System.Net;

namespace Payment.API.Tests;

/// <summary>
/// Step 7.4: API-key failure throttle (Strict-grade 429 on brute force),
/// short-TTL resolution cache, and revocation purge.
/// </summary>
public class SecretKeyAuthMiddlewareTests
{
    private sealed class StubMerchantService : IMerchantService
    {
        public int ResolveCalls;
        public Func<string, string, Result<MerchantKeyResolution>> Resolve = (_, _) =>
            Result<MerchantKeyResolution>.Failure(new Error("Payment.InvalidApiKey", "Invalid API key."));

        public Task<Result<string>> GetMerchantStatusAsync(Guid merchantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<string>.Success("Active"));
        public Task<Result<MerchantConfig>> GetMerchantConfigAsync(Guid merchantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<MerchantConfig>.Success(new MerchantConfig(null, false, null, null, null)));
        public Task<Result<Guid?>> GetMerchantOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<Guid?>.Success(null));
        public Task<Result<MerchantKeyResolution>> ResolveApiKeyAsync(string keyPrefix, string keyValue, CancellationToken cancellationToken = default)
        {
            ResolveCalls++;
            return Task.FromResult(Resolve(keyPrefix, keyValue));
        }
    }

    private sealed class TestOptionsMonitor : IOptionsMonitor<ApiKeyAuthOptions>
    {
        public TestOptionsMonitor(ApiKeyAuthOptions value) => CurrentValue = value;
        public ApiKeyAuthOptions CurrentValue { get; }
        public ApiKeyAuthOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<ApiKeyAuthOptions, string?> listener) => null;
    }

    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly TestOptionsMonitor _options = new(new ApiKeyAuthOptions
    {
        CacheTtlSeconds = 120,
        FailurePermitLimit = 20,
        FailureWindowSeconds = 60
    });

    private SecretKeyAuthMiddleware Create(StubMerchantService service) =>
        new(_ => Task.CompletedTask,
            NullLogger<SecretKeyAuthMiddleware>.Instance,
            new ApiKeyResolutionCache(_cache, _options),
            new ApiKeyFailureThrottle(_cache, _options));

    private static DefaultHttpContext PublicContext(string ip, string? apiKey)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/v1/payments/intents";
        ctx.Request.Method = "POST";
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        ctx.Response.Body = new MemoryStream();
        if (apiKey is not null)
            ctx.Request.Headers.Authorization = $"Bearer {apiKey}";
        return ctx;
    }

    [Fact]
    public async Task Invoke_NonPublicPath_SkipsAuth()
    {
        var nextCalled = false;
        var middleware = new SecretKeyAuthMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<SecretKeyAuthMiddleware>.Instance,
            new ApiKeyResolutionCache(_cache, _options),
            new ApiKeyFailureThrottle(_cache, _options));
        var service = new StubMerchantService();
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/v1/payments";
        ctx.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(ctx, service);

        Assert.True(nextCalled);
        Assert.Equal(0, service.ResolveCalls);
    }

    [Fact]
    public async Task Invoke_ValidKey_ResolvesCachesAndContinues()
    {
        var merchantId = Guid.NewGuid();
        var service = new StubMerchantService
        {
            Resolve = (_, _) => Result<MerchantKeyResolution>.Success(
                new MerchantKeyResolution(merchantId, "Active", "test"))
        };
        var middleware = Create(service);
        var ctx = PublicContext("10.0.0.1", "sk_test_abc123");

        await middleware.InvokeAsync(ctx, service);
        var second = PublicContext("10.0.0.1", "sk_test_abc123");
        await middleware.InvokeAsync(second, service);

        Assert.Equal(merchantId, ctx.Items["MerchantId"]);
        Assert.Equal(1, service.ResolveCalls); // second hit served from cache
        Assert.Equal(merchantId, second.Items["MerchantId"]);
    }

    [Fact]
    public async Task Invoke_InvalidKey_Returns401()
    {
        var middleware = Create(new StubMerchantService());
        var ctx = PublicContext("10.0.0.2", "sk_test_wrong");

        await middleware.InvokeAsync(ctx, new StubMerchantService());

        Assert.Equal(StatusCodes.Status401Unauthorized, ctx.Response.StatusCode);
        Assert.DoesNotContain("MerchantId", ctx.Items.Keys.Cast<string>());
    }

    [Fact]
    public async Task Invoke_TwentyFailuresThenOneMore_Returns429WithoutServiceCall()
    {
        var service = new StubMerchantService();
        var middleware = Create(service);
        const string ip = "10.0.0.3";

        for (var i = 0; i < 20; i++)
        {
            var ctx = PublicContext(ip, $"sk_test_bad-{i}");
            await middleware.InvokeAsync(ctx, service);
            Assert.Equal(StatusCodes.Status401Unauthorized, ctx.Response.StatusCode);
        }

        var throttled = PublicContext(ip, "sk_test_bad-final");
        await middleware.InvokeAsync(throttled, service);

        Assert.Equal(StatusCodes.Status429TooManyRequests, throttled.Response.StatusCode);
        Assert.Equal(20, service.ResolveCalls); // throttled request never reaches the service
    }

    [Fact]
    public async Task Invoke_ThrottledIp_DoesNotAffectOtherIp()
    {
        var service = new StubMerchantService();
        var middleware = Create(service);

        for (var i = 0; i < 21; i++)
            await middleware.InvokeAsync(PublicContext("10.0.0.4", $"sk_test_x-{i}"), service);

        var other = PublicContext("10.0.0.5", "sk_test_other");
        await middleware.InvokeAsync(other, service);

        Assert.Equal(StatusCodes.Status401Unauthorized, other.Response.StatusCode);
    }
}

public class ApiKeyResolutionCacheTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IOptionsMonitor<ApiKeyAuthOptions> _options =
        Mock.Of<IOptionsMonitor<ApiKeyAuthOptions>>(o =>
            o.CurrentValue == new ApiKeyAuthOptions { CacheTtlSeconds = 120 });

    private ApiKeyResolutionCache Create() => new(_cache, _options);

    [Fact]
    public void BuildCacheKey_StableAndDistinctPerKeyValue()
    {
        var a = ApiKeyResolutionCache.BuildCacheKey("sk_test_", "sk_test_same");
        var b = ApiKeyResolutionCache.BuildCacheKey("sk_test_", "sk_test_same");
        var c = ApiKeyResolutionCache.BuildCacheKey("sk_test_", "sk_test_other");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.DoesNotContain("sk_test_same", a); // raw key never in the cache key verbatim
    }

    [Fact]
    public void PurgeByMerchant_RemovesOnlyThatMerchant()
    {
        var cache = Create();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var k1 = ApiKeyResolutionCache.BuildCacheKey("sk_test_", "sk_test_k1");
        var k2 = ApiKeyResolutionCache.BuildCacheKey("sk_test_", "sk_test_k2");
        var k3 = ApiKeyResolutionCache.BuildCacheKey("sk_test_", "sk_test_k3");
        cache.Set(k1, new MerchantKeyResolution(m1, "Active", "test"));
        cache.Set(k2, new MerchantKeyResolution(m1, "Active", "test"));
        cache.Set(k3, new MerchantKeyResolution(m2, "Active", "test"));

        Assert.Equal(2, cache.PurgeByMerchant(m1));

        Assert.False(cache.TryGet(k1, out _));
        Assert.False(cache.TryGet(k2, out _));
        Assert.True(cache.TryGet(k3, out _));
    }

    [Fact]
    public void PurgeByMerchant_UnknownMerchant_ReturnsZero()
    {
        Assert.Equal(0, Create().PurgeByMerchant(Guid.NewGuid()));
    }
}
