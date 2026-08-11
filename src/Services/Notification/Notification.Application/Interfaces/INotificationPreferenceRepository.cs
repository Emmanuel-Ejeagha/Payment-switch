using Notification.Domain.Entities;

namespace Notification.Application.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> FindAsync(string recipient, string channel, string eventType, CancellationToken cancellationToken = default);
    Task<List<NotificationPreference>> ListByRecipientAsync(string recipient, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default);
}
