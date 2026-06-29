using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record PaymentIntentCreatedDomainEvent(
    Guid IntentId,
    Guid MerchantId,
    Money Amount,
    string IdempotencyKey) : DomainEvent;