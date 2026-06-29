using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

public record PaymentAuthorizedDomainEvent(
    Guid IntentId,
    string AuthorizationCode,
    Money Amount,
    string GatewayReference) : DomainEvent;