namespace Merchant.Application.Features.Commands.GenerateMerchantApiKey;

public record GenerateMerchantApiKeyResponse(Guid KeyId, string PlainTextKey, string Environment, DateTime CreatedAt);
