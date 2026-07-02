using BuildingBlocks.Shared.Results;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Application.Interfaces;

public interface INotificationSender
{
    Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default);
}