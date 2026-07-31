namespace Merchant.Application.DTOs;

public record MerchantApiKeyDto(
    Guid KeyId,
    string Environment,
    string KeyPrefix,
    DateTime CreatedAt,
    DateTime? RevokedAt
);

public record MerchantKeyResolution(
    Guid MerchantId,
    string Status,
    string Environment
);
