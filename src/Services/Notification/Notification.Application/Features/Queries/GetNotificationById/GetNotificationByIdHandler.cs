using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;


namespace Notification.Application.Features.Queries.GetNotificationById;

public class GetNotificationByIdHandler
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<GetNotificationByIdHandler> _logger;

    public GetNotificationByIdHandler(INotificationRepository repository, ILogger<GetNotificationByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<NotificationDto>> Handle(GetNotificationByIdQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName} with Id {Id}", nameof(GetNotificationByIdQuery), query.NotificationId);

        var notification = await _repository.GetByIdAsync(query.NotificationId, cancellationToken);
        if (notification is null)
            return new Error("Notification.NotFound", "Notification not found.");

        return Map(notification);
    }

    private static NotificationDto Map(NotificationEntity n) =>
        new(n.Id, n.Recipient, n.Channel.Value, n.Subject, n.Body, n.WebhookUrl,
            n.Status.Value, n.RetryCount, n.NextRetryAt, n.CreatedAt, n.ProcessedAt);
}
