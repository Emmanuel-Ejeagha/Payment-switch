namespace Merchant.Application.DTOs;

public record MerchantDto(
    Guid Id,
    string BusinessName,
    string Email,
    string Status,
    string? WebhookUrl,
    List<string> EnabledPaymentMethods,
    DateTime CreatedAt,
    bool AutoCapture = true,
    string? RejectionReason = null,
    string? SettlementBankAccountName = null,
    string? SettlementBankAccountNumber = null,
    string? SettlementBankName = null,
    string? SettlementCurrency = null,
    string? SettlementSchedule = null,
    string? ContactPhone = null,
    string? ContactAddress = null,
    string? ContactPerson = null
);