using BuildingBlocks.Shared.Events;

namespace Payment.Domain.DomainEvents;

public record SubscriptionPastDueDomainEvent(
    Guid SubscriptionId,
    Guid MerchantId,
    DateTime RetryAt) : DomainEvent;
