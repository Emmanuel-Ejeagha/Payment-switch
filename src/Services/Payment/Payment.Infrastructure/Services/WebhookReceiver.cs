using System.Collections.Concurrent;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Outcome of receiver-side webhook validation (Step 7.3).
/// Order of checks: freshness → signature (current, then previous in grace)
/// → delivery-id idempotency. Invalid deliveries never consume an id.
/// </summary>
public enum WebhookValidationResult
{
    Valid,
    MissingSignature,
    MalformedTimestamp,
    StaleTimestamp,
    SignatureMismatch,
    MissingDeliveryId,
    DuplicateDelivery
}

/// <summary>
/// Minimal delivery-id store contract for webhook idempotency.
/// Merchants should back this with persistent storage (DB/Redis) so a restart
/// cannot reprocess a delivery; the in-memory implementation below is the
/// reference for samples and tests.
/// </summary>
public interface IDeliveryIdStore
{
    /// <summary>Tries to claim <paramref name="deliveryId"/>. False when already seen.</summary>
    bool TryClaim(string deliveryId);

    bool Contains(string deliveryId);
}

/// <summary>
/// Thread-safe in-memory delivery-id store with TTL expiry (default 72h, covers
/// the 8-retry backoff plus manual replay). Expired ids are pruned lazily on
/// claim and via <see cref="PruneExpired"/>.
/// </summary>
public sealed class InMemoryDeliveryIdStore : IDeliveryIdStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new(StringComparer.Ordinal);
    private readonly TimeSpan _ttl;

    public InMemoryDeliveryIdStore(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromHours(72);
    }

    public bool TryClaim(string deliveryId)
    {
        if (string.IsNullOrWhiteSpace(deliveryId))
            return false;

        PruneExpired();
        return _seen.TryAdd(deliveryId, DateTimeOffset.UtcNow + _ttl);
    }

    public bool Contains(string deliveryId) =>
        !string.IsNullOrWhiteSpace(deliveryId) &&
        _seen.TryGetValue(deliveryId, out var expiresAt) &&
        expiresAt > DateTimeOffset.UtcNow;

    public int Count
    {
        get
        {
            PruneExpired();
            return _seen.Count;
        }
    }

    public void PruneExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _seen)
        {
            if (kv.Value <= now)
                _seen.TryRemove(kv.Key, out _);
        }
    }
}

/// <summary>
/// Receiver-side webhook validator (Step 7.3): enforces the 5-minute freshness
/// window, verifies HMAC against the current secret (then the previous secret
/// inside the rotation grace window), and dedupes on X-PaymentSwitch-Delivery.
/// </summary>
public static class WebhookReceiver
{
    public static WebhookValidationResult Validate(
        string? currentSecret,
        string? previousSecret,
        DateTime? rotatedAtUtc,
        byte[] payload,
        string timestamp,
        string signature,
        string deliveryId,
        IDeliveryIdStore store,
        TimeSpan gracePeriod,
        TimeSpan? maxAge = null,
        DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return WebhookValidationResult.MissingSignature;

        if (!long.TryParse(timestamp, out var tsUnix))
            return WebhookValidationResult.MalformedTimestamp;

        var nowUnix = new DateTimeOffset(nowUtc ?? DateTime.UtcNow).ToUnixTimeSeconds();
        if (Math.Abs(nowUnix - tsUnix) > (maxAge ?? WebhookSignature.DefaultFreshnessWindow).TotalSeconds)
            return WebhookValidationResult.StaleTimestamp;

        if (string.IsNullOrWhiteSpace(deliveryId))
            return WebhookValidationResult.MissingDeliveryId;

        var ok = WebhookSignature.VerifyWithRotation(
            currentSecret, previousSecret, rotatedAtUtc,
            payload, timestamp, signature, gracePeriod, maxAge, nowUtc);
        if (!ok)
            return WebhookValidationResult.SignatureMismatch;

        return store.TryClaim(deliveryId)
            ? WebhookValidationResult.Valid
            : WebhookValidationResult.DuplicateDelivery;
    }
}
