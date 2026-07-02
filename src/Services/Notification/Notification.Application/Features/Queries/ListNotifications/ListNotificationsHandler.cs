using BuildingBlocks.Shared.Results;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;

namespace Notification.Application.Features.Queries.ListNotifications;

public class ListNotificationsHandler
{
    private readonly INotificationRepository _repository;

    public ListNotificationsHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<NotificationDto>>> Handle(ListNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.ListAsync(
            query.Recipient, query.Channel, query.Status,
            query.Skip, query.Take, cancellationToken);

        return notifications;
    }
}