using BuildingBlocks.Shared.Events;

namespace Payment.Domain.DomainEvents;

public record SubscriptionCanceledDomainEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    Guid CustomerId,
    DateTime CanceledAt) : DomainEvent;
