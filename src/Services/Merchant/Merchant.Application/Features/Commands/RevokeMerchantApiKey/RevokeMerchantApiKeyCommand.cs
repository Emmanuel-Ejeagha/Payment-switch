namespace Merchant.Application.Features.Commands.RevokeMerchantApiKey;

public record RevokeMerchantApiKeyCommand(Guid MerchantId, Guid KeyId);
