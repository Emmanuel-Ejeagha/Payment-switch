using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record PaymentExpiredDomainEvent(Guid IntentId, Guid MerchantId, Money Amount) : DomainEvent;