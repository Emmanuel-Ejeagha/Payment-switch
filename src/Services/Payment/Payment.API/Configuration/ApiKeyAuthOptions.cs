namespace Payment.API.Configuration;

/// <summary>
/// API-key auth tuning (Step 7.4, optional <c>ApiKeyAuth</c> section).
/// The resolution cache is deliberately short-lived so a revoked key stops
/// working quickly even if the explicit purge notify is missed; the purge
/// path (<c>POST /api/v1/internal/apikeys/purge</c>, called best-effort by
/// Merchant on revoke) makes revocation effective immediately.
/// </summary>
public sealed class ApiKeyAuthOptions
{
    public const string SectionName = "ApiKeyAuth";

    /// <summary>Positive resolution TTL. Default 120s (was a hardcoded 300s).</summary>
    public int CacheTtlSeconds { get; set; } = 120;

    /// <summary>Failed attempts per <see cref="FailureWindowSeconds"/> per IP before 429.</summary>
    public int FailurePermitLimit { get; set; } = 20;

    /// <summary>Failure counting window. Default 60s.</summary>
    public int FailureWindowSeconds { get; set; } = 60;
}
