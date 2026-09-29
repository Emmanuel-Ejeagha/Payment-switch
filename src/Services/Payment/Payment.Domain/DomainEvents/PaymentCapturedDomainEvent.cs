using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record PaymentCapturedDomainEvent(
    Guid IntentId,
    Guid MerchantId,
    Guid TransactionId,
    Money Amount) : DomainEvent;