namespace Payment.Application.DTOs;

public record MerchantConfig(string? WebhookUrl, bool AutoCapture);
