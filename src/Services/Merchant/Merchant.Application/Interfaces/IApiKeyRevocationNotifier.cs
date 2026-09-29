namespace Merchant.Application.Interfaces;

/// <summary>
/// Best-effort notify that an API key was revoked so Payment can purge its
/// short-TTL resolution cache immediately (Step 7.4). Implementations must
/// never throw — a missed notify is backstopped by the cache TTL.
/// </summary>
public interface IApiKeyRevocationNotifier
{
    Task NotifyRevokedAsync(Guid merchantId, CancellationToken cancellationToken = default);
}
