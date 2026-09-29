using BuildingBlocks.Shared.Events;

namespace Payment.Domain.DomainEvents;

public record SubscriptionCreatedDomainEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid CustomerId,
    Guid PlanId,
    string Code) : DomainEvent;
