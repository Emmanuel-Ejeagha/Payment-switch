using Notification.Application.DTOs;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Application.Interfaces;

public interface INotificationRepository
{
    Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
    Task<List<NotificationEntity>> GetPendingForRetryAsync(DateTime now, int batchSize, CancellationToken cancellationToken = default);
    Task<List<NotificationDto>> ListAsync(string? recipient, string? channel, string? status, int skip, int take, CancellationToken cancellationToken = default);
}