using BuildingBlocks.Shared.Events;

namespace Payment.Domain.DomainEvents;

public record PaymentVoidedDomainEvent(Guid IntentId) : DomainEvent;