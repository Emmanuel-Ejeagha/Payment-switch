using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Notification.Application.Interfaces;
using Notification.Application.Templates;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class EmailSender : INotificationSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly SmtpSettings _settings;
    private readonly IHostEnvironment _environment;

    public EmailSender(ILogger<EmailSender> logger, IOptions<SmtpSettings> settings, IHostEnvironment environment)
    {
        _logger = logger;
        _settings = settings.Value;
        _environment = environment;
    }

    public async Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            // Simulation is a development convenience only. In any other
            // environment an unconfigured SMTP relay is a deployment error and
            // must surface as a failure rather than silently pretending to send.
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("SIMULATED EMAIL (Smtp:Host not configured): To={Recipient}, Subject={Subject}, Body={Body}",
                    DataMasker.MaskEmail(notification.Recipient), notification.Subject, notification.Body);
                return Result.Success();
            }

            _logger.LogError("EMAIL NOT SENT: Smtp:Host is not configured in a non-Development environment. To={Recipient}, Subject={Subject}",
                DataMasker.MaskEmail(notification.Recipient), notification.Subject);
            return new Error("Email.NotConfigured", "SMTP is not configured. Set Smtp:Host (Smtp__Host) before enabling email delivery.");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress("", notification.Recipient));
            message.Subject = notification.Subject ?? "No subject";

            var body = new BodyBuilder { TextBody = notification.Body ?? notification.Payload };
            body.HtmlBody = EmailTemplateRenderer.Render(notification.Subject, notification.Body);
            message.Body = body.ToMessageBody();

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
