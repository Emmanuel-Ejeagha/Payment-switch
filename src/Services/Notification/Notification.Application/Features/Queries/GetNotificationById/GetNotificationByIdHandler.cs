using BuildingBlocks.Shared.Results;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;


namespace Notification.Application.Features.Queries.GetNotificationById;

public class GetNotificationByIdHandler
{
    private readonly INotificationRepository _repository;

    public GetNotificationByIdHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<NotificationDto>> Handle(GetNotificationByIdQuery query, CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(query.NotificationId, cancellationToken);
        if (notification is null)
            return new Error("Notification.NotFound", "Notification not found.");

        return Map(notification);
    }

    private static NotificationDto Map(NotificationEntity n) =>
        new(n.Id, n.Recipient, n.Channel.Value, n.Subject, n.Body, n.WebhookUrl,
            n.Status.Value, n.RetryCount, n.NextRetryAt, n.CreatedAt, n.ProcessedAt);
}
