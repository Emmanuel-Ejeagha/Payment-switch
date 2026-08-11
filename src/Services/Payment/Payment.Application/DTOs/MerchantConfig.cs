namespace Payment.Application.DTOs;

public record MerchantConfig(
    string? WebhookUrl,
    bool AutoCapture,
    string? WebhookSecret = null,
    string? PreviousWebhookSecret = null,
    DateTime? WebhookSecretRotatedAtUtc = null);
