using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Interfaces;
using Notification.Application.Messaging;
using Notification.Application.Services;
using Notification.Application.Templates;
using NotificationEntity = Notification.Domain.Entities.Notification;
using Resend;
using ResendMessage = Resend.EmailMessage;

namespace Notification.Infrastructure.Senders;

/// <summary>
/// Resend-backed email sender for verification mail (transactional).
/// SMTP keeps serving every other email channel; this sender is selected only
/// for notifications carrying the <c>resend</c> provider hint.
/// When Resend is not configured the email is NOT silently dropped: in
/// Development it is simulated in logs (never with the token), elsewhere it
/// fails loudly so delivery is never falsely reported.
/// </summary>
public class ResendEmailSender : INotificationSender
{
    private readonly ILogger<ResendEmailSender> _logger;
    private readonly ResendSettings _settings;
    private readonly IResend _resend;
    private readonly Application.Configuration.VerificationEmailOptions _linkOptions;
    private readonly IHostEnvironment _environment;

    public ResendEmailSender(
        ILogger<ResendEmailSender> logger,
        IOptions<ResendSettings> settings,
        IResend resend,
        IOptions<Application.Configuration.VerificationEmailOptions> linkOptions,
        IHostEnvironment environment)
    {
        _logger = logger;
        _settings = settings.Value;
        _resend = resend;
        _linkOptions = linkOptions.Value;
        _environment = environment;
    }

    public async Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsConfigured)
        {
            // Simulation is a development convenience only. In any other
            // environment an unconfigured Resend token is a deployment error.
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("SIMULATED RESEND EMAIL (Resend:ApiKey not configured): To={Recipient}, Subject={Subject}",
                    DataMasker.MaskEmail(notification.Recipient), notification.Subject);
                return Result.Success();
            }

            _logger.LogError("RESEND EMAIL NOT SENT: Resend:ApiKey is not configured in a non-Development environment. To={Recipient}, Subject={Subject}",
                DataMasker.MaskEmail(notification.Recipient), notification.Subject);
            return new Error("Email.NotConfigured", "Resend is not configured. Set Resend:ApiKey (Resend__ApiKey) before enabling email delivery.");
        }

        EmailVerificationRequestedEvent verificationEvent;
        try
        {
            verificationEvent = System.Text.Json.JsonSerializer.Deserialize<EmailVerificationRequestedEvent>(notification.Payload)
                ?? throw new InvalidOperationException("Empty verification payload.");
            if (string.IsNullOrWhiteSpace(verificationEvent.Token) || string.IsNullOrWhiteSpace(verificationEvent.Email))
                throw new InvalidOperationException("Verification payload is missing token or email.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Poison payload: runs the standard retry budget, then DLQ.
            _logger.LogError(ex, "RESEND EMAIL FAILED: malformed verification payload for {Recipient}", DataMasker.MaskEmail(notification.Recipient));
            return new Error("Email.InvalidPayload", "Verification payload could not be read.");
        }

        var content = VerificationEmailBuilder.Build(
            verificationEvent.Email,
            verificationEvent.Token,
            verificationEvent.IssuedAtUtc,
            verificationEvent.ExpiresAtUtc,
            notification.Subject ?? _linkOptions.Subject,
            _linkOptions.FrontendBaseUrl);

        if (string.IsNullOrEmpty(content.Link))
        {
            _logger.LogError("RESEND EMAIL NOT SENT: verification link base URL is not configured. To={Recipient}",
                DataMasker.MaskEmail(notification.Recipient));
            return new Error("Email.NotConfigured", "Verification link base URL is not configured.");
        }

        var message = new ResendMessage
        {
            From = new EmailAddress { Email = _settings.FromEmail, DisplayName = _settings.FromName },
            Subject = content.Subject,
            TextBody = content.TextBody,
            HtmlBody = EmailTemplateRenderer.RenderVerification(content)
        };
        message.To.Add(verificationEvent.Email);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds)));

            var response = await _resend.EmailSendAsync(message, timeoutCts.Token);
            if (!response.Success || response.Exception is not null)
            {
                var transient = response.Exception?.IsTransient ?? true;
                _logger.LogError("RESEND EMAIL FAILED: To={Recipient}, Subject={Subject}, Transient={Transient}, StatusCode={StatusCode}",
                    DataMasker.MaskEmail(notification.Recipient), content.Subject, transient, response.Exception?.StatusCode);
                return new Error(transient ? "Email.TransientFailure" : "Email.ProviderRejected",
                    transient ? "Email delivery failed transiently." : "Email was rejected by the provider.");
            }

            _logger.LogInformation("RESEND EMAIL SENT: To={Recipient}, Subject={Subject}, ProviderId={ProviderId}",
                DataMasker.MaskEmail(notification.Recipient), content.Subject, response.Content);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Our own timeout fired (caller's token is fine): transient.
            _logger.LogWarning("RESEND EMAIL TIMEOUT: To={Recipient}, Subject={Subject}",
                DataMasker.MaskEmail(notification.Recipient), content.Subject);
            return new Error("Email.TransientFailure", "Email delivery timed out.");
        }
        catch (ResendException ex)
        {
            _logger.LogError(ex, "RESEND EMAIL FAILED: To={Recipient}, Subject={Subject}, Transient={Transient}, StatusCode={StatusCode}",
                DataMasker.MaskEmail(notification.Recipient), content.Subject, ex.IsTransient, ex.StatusCode);
            return new Error(ex.IsTransient ? "Email.TransientFailure" : "Email.ProviderRejected",
                ex.IsTransient ? "Email delivery failed transiently." : "Email was rejected by the provider.");
        }
        // codeql[cs/catch-of-all-exceptions]: provider failures become a
        // bounded Result error (retry budget + DLQ), never an exception.
        catch (Exception ex)
        {
            _logger.LogError(ex, "RESEND EMAIL FAILED: To={Recipient}, Subject={Subject}",
                DataMasker.MaskEmail(notification.Recipient), content.Subject);
            return new Error("Email.SendFailed", "Email delivery failed.");
        }
    }
}
