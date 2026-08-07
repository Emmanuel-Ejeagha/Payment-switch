namespace Merchant.Application.DTOs;

public record MerchantDto(
    Guid Id,
    string BusinessName,
    string Email,
    string Status,
    string? WebhookUrl,
    string? WebhookSecret,
    List<string> EnabledPaymentMethods,
    DateTime CreatedAt,
    bool AutoCapture = true
);