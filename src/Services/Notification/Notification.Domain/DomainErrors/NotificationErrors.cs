
using BuildingBlocks.Shared.Results;

namespace Notification.Domain.DomainErrors;

public static class NotificationErrors
{
    public static Error InvalidRecipient =>
        new("Notification.InvalidRecipient", "Recipient cannot be empty.");

    public static Error InvalidChannel =>
        new("Notification.InvalidChannel", "Invalid notification channel.");

    public static Error InvalidEventType =>
        new("Notification.InvalidEventType", "Invalid notification event type.");

    public static Error RecipientRequired =>
        new("Notification.RecipientRequired", "Recipient is required.");
}