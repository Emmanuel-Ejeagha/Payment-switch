using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class EmailSender : INotificationSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly SmtpSettings _settings;

    public EmailSender(ILogger<EmailSender> logger, IOptions<SmtpSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogInformation("SIMULATED EMAIL: To={Recipient}, Subject={Subject}",
                DataMasker.MaskEmail(notification.Recipient), notification.Subject);
            return Result.Success();
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress("", notification.Recipient));
            message.Subject = notification.Subject ?? "No subject";
            message.Body = new TextPart("plain") { Text = notification.Body ?? notification.Payload };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("EMAIL SENT: To={Recipient}, Subject={Subject}", DataMasker.MaskEmail(notification.Recipient), notification.Subject);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EMAIL FAILED: To={Recipient}, Subject={Subject}", DataMasker.MaskEmail(notification.Recipient), notification.Subject);
            return new Error("Email.SendFailed", $"Failed to send email: {ex.Message}");
        }
    }
}
