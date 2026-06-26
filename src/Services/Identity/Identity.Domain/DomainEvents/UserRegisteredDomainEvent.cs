using BuildingBlocks.Shared.Events;

namespace Identity.Domain.DomainEvents;

public record UserRegisteredDomainEvent(Guid UserId, string Email, string FullName) : DomainEvent;