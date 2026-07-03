using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class EmailSender : INotificationSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SIMULATED EMAIL: To={Recipient}, Subject={Subject}, Body={Body}",
            notification.Recipient, notification.Subject, notification.Body);
        return Task.FromResult(Result.Success());
    }
}