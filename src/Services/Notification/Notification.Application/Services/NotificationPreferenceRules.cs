using Notification.Domain.Entities;

namespace Notification.Application.Services;

/// <summary>
/// Enforcement rule for notification preferences: an explicit row that is
/// disabled suppresses the event; absent rows default to allowed (opt-in model).
/// </summary>
public static class NotificationPreferenceRules
{
    public static bool IsSuppressed(NotificationPreference? preference) =>
        preference is not null && !preference.Enabled;
}
