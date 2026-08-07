namespace Merchant.API.Contracts;

public record UpdateMerchantConfigurationRequest(string? WebhookUrl, List<string>? PaymentMethods, bool? AutoCapture);
