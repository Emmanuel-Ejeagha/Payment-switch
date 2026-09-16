using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Payment.API.Configuration;
using Payment.Application.DTOs;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Payment.API.Middlewares;

/// <summary>
/// Short-TTL cache for secret-key resolutions with revocation purge (Step 7.4).
/// Entries are keyed by key-prefix + SHA-256 of the presented key (the raw key
/// never sits in the cache key verbatim) and indexed by merchant so
/// <see cref="PurgeByMerchant"/> can drop every entry for a merchant when
/// Merchant notifies a revocation. Stale reverse-index entries are pruned
/// lazily on purge.
/// </summary>
public sealed class ApiKeyResolutionCache
{
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<ApiKeyAuthOptions> _options;
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _byMerchant = new();

    public ApiKeyResolutionCache(IMemoryCache cache, IOptionsMonitor<ApiKeyAuthOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    public static string BuildCacheKey(string keyPrefix, string apiKey) =>
        $"apikey:{keyPrefix}:{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)))}";

    public bool TryGet(string cacheKey, out MerchantKeyResolution? resolution) =>
        _cache.TryGetValue(cacheKey, out resolution);

    public void Set(string cacheKey, MerchantKeyResolution resolution)
    {
        _cache.Set(cacheKey, resolution, TimeSpan.FromSeconds(_options.CurrentValue.CacheTtlSeconds));
        _byMerchant.GetOrAdd(resolution.MerchantId, _ => new ConcurrentDictionary<string, byte>())
            .TryAdd(cacheKey, 0);
    }

    /// <summary>Removes every cached resolution for <paramref name="merchantId"/>. Returns the count removed.</summary>
    public int PurgeByMerchant(Guid merchantId)
    {
        if (!_byMerchant.TryRemove(merchantId, out var keys))
            return 0;

        var removed = 0;
        foreach (var key in keys.Keys)
        {
            _cache.Remove(key);
            removed++;
        }
        return removed;
    }
}

/// <summary>
/// Per-IP failure throttle for secret-key auth (Step 7.4). The middleware
/// short-circuits invalid keys with 401 before endpoint rate-limiting
/// policies engage, so failures are counted here: past
/// <c>FailurePermitLimit</c> failures inside <c>FailureWindowSeconds</c> the
/// caller gets 429 instead of another Merchant gRPC/BCrypt round-trip.
/// Successes are never counted.
/// </summary>
public sealed class ApiKeyFailureThrottle
{
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<ApiKeyAuthOptions> _options;

    public ApiKeyFailureThrottle(IMemoryCache cache, IOptionsMonitor<ApiKeyAuthOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    public static string BuildThrottleKey(string partitionKey) => $"apikey-fail:{partitionKey}";

    public bool IsThrottled(string partitionKey) =>
        _cache.TryGetValue<int>(BuildThrottleKey(partitionKey), out var count) &&
        count >= _options.CurrentValue.FailurePermitLimit;

    public void RecordFailure(string partitionKey)
    {
        var key = BuildThrottleKey(partitionKey);
        var count = _cache.TryGetValue<int>(key, out var current) ? current : 0;
        _cache.Set(key, count + 1, TimeSpan.FromSeconds(_options.CurrentValue.FailureWindowSeconds));
    }
}
