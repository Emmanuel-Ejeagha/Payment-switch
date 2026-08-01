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
}
