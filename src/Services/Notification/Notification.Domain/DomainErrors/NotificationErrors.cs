
using BuildingBlocks.Shared.Results;

namespace Notification.Domain.DomainErrors;

public static class NotificationErrors
{
    public static Error InvalidRecipient =>
        new("Notification.InvalidRecipient", "Recipient cannot be empty.");

    public static Error InvalidChannel =>
        new("Notification.InvalidChannel", "Invalid notification channel.");
}