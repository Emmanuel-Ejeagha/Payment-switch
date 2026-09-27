using Payment.Application.DTOs;
using Payment.Domain.Entities;

namespace Payment.Application.Mappings;

public static class BillingMappings
{
    public static PlanDto ToDto(this Plan plan) => new(
        plan.Id,
        plan.MerchantId,
        plan.Code,
        plan.Name,
        plan.Amount.Amount,
        plan.Amount.Currency,
        plan.Interval.Unit,
        plan.Interval.Count,
        plan.Description,
        plan.Active,
        plan.CreatedAt);

    public static SubscriptionDto ToDto(this Subscription subscription) => new(
        subscription.Id,
        subscription.MerchantId,
        subscription.CustomerId,
        subscription.PlanId,
        subscription.Code,
        subscription.Status,
        subscription.CurrentPeriodStart,
        subscription.CurrentPeriodEnd,
        subscription.NextBillingAt,
        subscription.CancelAtPeriodEnd,
        subscription.CanceledAt,
        subscription.CreatedAt);

    public static InvoiceDto ToDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.MerchantId,
        invoice.CustomerId,
        invoice.SubscriptionId,
        invoice.Code,
        invoice.Amount.Amount,
        invoice.Amount.Currency,
        invoice.Status,
        invoice.PeriodStart,
        invoice.PeriodEnd,
        invoice.PaymentIntentId,
        invoice.AttemptCount,
        invoice.LastError,
        invoice.PaidAt,
        invoice.CreatedAt);
}
