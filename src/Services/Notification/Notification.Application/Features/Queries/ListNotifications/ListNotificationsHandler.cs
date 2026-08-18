using BuildingBlocks.Shared.Paging;
using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;

namespace Notification.Application.Features.Queries.ListNotifications;

public class ListNotificationsHandler
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<ListNotificationsHandler> _logger;

    public ListNotificationsHandler(INotificationRepository repository, ILogger<ListNotificationsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PagedData<NotificationDto>>> Handle(ListNotificationsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName}", nameof(ListNotificationsQuery));

        var notifications = await _repository.ListAsync(
            query.Recipient, query.Channel, query.Status,
            query.Skip, query.Take, cancellationToken);

        var total = await _repository.CountAsync(
            query.Recipient, query.Channel, query.Status, cancellationToken);

        return new PagedData<NotificationDto>(notifications, total);
    }
}
