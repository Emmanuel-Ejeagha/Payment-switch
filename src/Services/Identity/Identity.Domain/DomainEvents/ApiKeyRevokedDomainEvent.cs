using BuildingBlocks.Shared.Events;

namespace Identity.Domain.DomainEvents;

public record ApiKeyRevokedDomainEvent(Guid UserId, Guid ApiKeyId) : DomainEvent;