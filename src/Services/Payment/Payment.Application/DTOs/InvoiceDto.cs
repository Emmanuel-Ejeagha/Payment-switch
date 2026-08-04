namespace Payment.Application.DTOs;

public record InvoiceDto(
    Guid Id,
    Guid MerchantId,
    Guid CustomerId,
    Guid SubscriptionId,
    string Code,
    long Amount,
    string Currency,
    string Status,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    Guid? PaymentIntentId,
    int AttemptCount,
    string? LastError,
    DateTime? PaidAt,
    DateTime CreatedAt);
