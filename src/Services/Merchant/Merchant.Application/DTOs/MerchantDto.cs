namespace Merchant.Application.DTOs;

public record MerchantDto(
    Guid Id,
    string BusinessName,
    string Email,
    string Status,
    string? WebhookUrl,
    List<string> EnabledPaymentMethods
);