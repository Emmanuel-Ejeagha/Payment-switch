namespace Merchant.Application.Features.Commands.GenerateMerchantApiKey;

public record GenerateMerchantApiKeyCommand(Guid MerchantId, string Environment);
