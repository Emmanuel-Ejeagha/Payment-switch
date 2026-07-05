using BuildingBlocks.Shared.Events;

namespace Notification.Domain.DomainEvents;

public record NotificationSentEvent(Guid NotificationId, string Recipient, string Channel) : DomainEvent;