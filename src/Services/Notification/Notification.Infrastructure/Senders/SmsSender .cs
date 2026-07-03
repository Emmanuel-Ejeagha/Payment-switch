using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class SmsSender : INotificationSender
{
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(ILogger<SmsSender> logger)
    {
        _logger = logger;
    }

    public Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SIMULATED SMS: To={Recipient}, Body={Body}",
            notification.Recipient, notification.Body);
        return Task.FromResult(Result.Success());
    }
}