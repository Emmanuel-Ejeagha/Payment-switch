using BuildingBlocks.Shared.Results;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class NotificationSenderDispatcher : INotificationSender
{
    private readonly EmailSender _emailSender;
    private readonly SmsSender _smsSender;
    private readonly WebhookSender _webhookSender;

    public NotificationSenderDispatcher(EmailSender emailSender, SmsSender smsSender, WebhookSender webhookSender)
    {
        _emailSender = emailSender;
        _smsSender = smsSender;
        _webhookSender = webhookSender;
    }

    public Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        return notification.Channel.Value switch
        {
            "email" => _emailSender.SendAsync(notification, cancellationToken),
            "sms" => _smsSender.SendAsync(notification, cancellationToken),
            "webhook" => _webhookSender.SendAsync(notification, cancellationToken),
            _ => throw new ArgumentException($"Unsupported channel: {notification.Channel.Value}")
        };
    }
}