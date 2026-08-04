using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record InvoiceIssuedDomainEvent(
    Guid InvoiceId,
    Guid MerchantId,
    Guid CustomerId,
    Guid SubscriptionId,
    Money Amount,
    string Code) : DomainEvent;
