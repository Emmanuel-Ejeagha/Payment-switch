using BuildingBlocks.Shared.Email;
using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Identity.Infrastructure.Services;

/// <summary>
/// SMTP email sender for transactional Identity mail (verification).
/// When SMTP is not configured the email is NOT silently dropped, but message
/// bodies are never logged: tokens live there (see <see cref="EmailLogRedactor"/>).
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly SmtpSettings _settings;

    public EmailSender(ILogger<EmailSender> logger, IOptions<SmtpSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            _logger.LogWarning(
                "SIMULATED EMAIL (Smtp:Host not configured): To={Recipient}, Subject={Subject}, BodyHash={BodyHash}",
                DataMasker.MaskEmail(message.To), message.Subject, EmailLogRedactor.Redact(message.TextBody));
            return Result.Success();
        }

        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            mime.To.Add(new MailboxAddress("", message.To));
            mime.Subject = message.Subject;

            var body = new BodyBuilder { TextBody = message.TextBody };
            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
                body.HtmlBody = message.HtmlBody;
            mime.Body = body.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("EMAIL SENT: To={Recipient}, Subject={Subject}", DataMasker.MaskEmail(message.To), message.Subject);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EMAIL FAILED: To={Recipient}, Subject={Subject}", DataMasker.MaskEmail(message.To), message.Subject);
            return new Error("Email.SendFailed", $"Failed to send email: {ex.Message}");
        }
    }
}
