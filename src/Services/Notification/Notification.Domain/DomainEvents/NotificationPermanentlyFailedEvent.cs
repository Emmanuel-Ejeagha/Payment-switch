using BuildingBlocks.Shared.Events;

namespace Notification.Domain.DomainEvents;

public record NotificationPermanentlyFailedEvent(Guid NotificationId, string Recipient, string Channel) : DomainEvent;