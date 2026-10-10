namespace BuildingBlocks.Shared.Auth;

public sealed class ServiceTokenOptions
{
    public const string SectionName = "ServiceToken";

    public string ServiceName { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 10;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public const string ClientTypeClaim = "client_type";

    public const string ClientTypeService = "service";

    /// <summary>
    /// Authentication scheme that validates service-to-service JWTs minted by
    /// <see cref="ServiceTokenProvider"/> (signed with the dedicated
    /// <c>ServiceToken:Secret</c>). The <c>ServiceOnly</c> authorization policy
    /// must list this scheme explicitly: without it, service tokens are
    /// evaluated against the user-JWT scheme, fail signature validation, and
    /// every service-to-service call 401s.
    /// </summary>
    public const string AuthenticationScheme = "ServiceToken";
}
