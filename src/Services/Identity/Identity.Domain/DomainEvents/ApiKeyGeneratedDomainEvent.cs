using BuildingBlocks.Shared.Events;

namespace Identity.Domain.DomainEvents;

public record ApiKeyGeneratedDomainEvent(Guid UserId, Guid ApiKeyId, string Environment) : DomainEvent;