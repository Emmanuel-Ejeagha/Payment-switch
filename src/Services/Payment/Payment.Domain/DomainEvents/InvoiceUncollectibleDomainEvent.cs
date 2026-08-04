using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record InvoiceUncollectibleDomainEvent(
    Guid InvoiceId,
    Guid MerchantId,
    Guid CustomerId,
    Guid SubscriptionId,
    Money Amount) : DomainEvent;
