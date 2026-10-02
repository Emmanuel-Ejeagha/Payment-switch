using BuildingBlocks.Shared.Results;
using Notification.Application.Interfaces;
using Notification.Domain;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class NotificationSenderDispatcher : INotificationSender
{
    private readonly EmailSender _emailSender;
    private readonly ResendEmailSender _resendEmailSender;
    private readonly SmsSender _smsSender;
    private readonly WebhookSender _webhookSender;

    public NotificationSenderDispatcher(EmailSender emailSender, ResendEmailSender resendEmailSender, SmsSender smsSender, WebhookSender webhookSender)
    {
        _emailSender = emailSender;
        _resendEmailSender = resendEmailSender;
        _smsSender = smsSender;
        _webhookSender = webhookSender;
    }

    public Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        return notification.Channel.Value switch
        {
            // Verification mail carries the resend provider hint; every other
            // email keeps the existing SMTP path.
            "email" when notification.Provider == NotificationProviders.Resend =>
                _resendEmailSender.SendAsync(notification, cancellationToken),
            "email" => _emailSender.SendAsync(notification, cancellationToken),
            "sms" => _smsSender.SendAsync(notification, cancellationToken),
            "webhook" => _webhookSender.SendAsync(notification, cancellationToken),
            _ => throw new ArgumentException($"Unsupported channel: {notification.Channel.Value}")
        };
    }
}