using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record PaymentRequiresActionDomainEvent(
    Guid IntentId,
    Guid MerchantId,
    Money Amount,
    string GatewayReference) : DomainEvent;
