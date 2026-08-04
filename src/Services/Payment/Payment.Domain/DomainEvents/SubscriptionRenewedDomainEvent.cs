using BuildingBlocks.Shared.Events;

namespace Payment.Domain.DomainEvents;

public record SubscriptionRenewedDomainEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd) : DomainEvent;
