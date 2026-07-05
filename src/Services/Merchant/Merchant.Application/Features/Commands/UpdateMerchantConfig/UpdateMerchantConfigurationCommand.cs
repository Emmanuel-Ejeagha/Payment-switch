namespace Merchant.Application.Features.Commands.UpdateMerchantConfig;

public record UpdateMerchantConfigurationCommand(
    Guid MerchantId,
    string? WebhookUrl,
    List<string>? PaymentMethods
);
