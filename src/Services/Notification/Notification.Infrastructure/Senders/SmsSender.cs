using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class SmsSender : INotificationSender
{
    private readonly ILogger<SmsSender> _logger;
    private readonly SmsSettings _settings;

    public SmsSender(ILogger<SmsSender> logger, IOptions<SmsSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        // Explicitly degraded channel: no provider is configured by default and
        // no producer creates SMS notifications yet. Fail the delivery so a
        // message that must go out over SMS is not silently dropped.
        if (!_settings.Enabled)
        {
            _logger.LogWarning("SMS NOT SENT: SMS channel is disabled (Sms:Enabled=false). To={Recipient}, Provider={Provider}",
                DataMasker.MaskEmail(notification.Recipient), _settings.Provider);
            return Task.FromResult<Result>(new Error("Sms.Disabled", "The SMS channel is disabled. Set Sms:Enabled=true and configure a provider to enable it."));
        }

        _logger.LogInformation("SMS NOT SENT: SMS provider '{Provider}' is not implemented. To={Recipient}",
            _settings.Provider, DataMasker.MaskEmail(notification.Recipient));
        return Task.FromResult<Result>(new Error("Sms.NotImplemented", $"The configured SMS provider '{_settings.Provider}' is not implemented."));
    }
}
