namespace Payment.Application.DTOs;

public record SubscriptionDto(
    Guid Id,
    Guid MerchantId,
    Guid CustomerId,
    Guid PlanId,
    string Code,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime? NextBillingAt,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAt,
    DateTime CreatedAt);
