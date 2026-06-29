using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record PaymentRefundedDomainEvent(
    Guid IntentId,
    Guid TransactionId,
    Money Amount) : DomainEvent;