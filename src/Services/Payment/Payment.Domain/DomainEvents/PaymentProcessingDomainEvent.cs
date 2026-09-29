using BuildingBlocks.Shared.Events;

namespace Payment.Domain.DomainEvents;

public record PaymentProcessingDomainEvent(
    Guid IntentId,
    Guid MerchantId,
    string Status) : DomainEvent;
