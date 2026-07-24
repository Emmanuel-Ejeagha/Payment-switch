using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Senders;

public class WebhookSender : INotificationSender
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookSender> _logger;

    public WebhookSender(HttpClient httpClient, ILogger<WebhookSender> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result> SendAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SIMULATED WEBHOOK: URL={Url}, Payload={Payload}",
            notification.WebhookUrl, notification.Payload);
        // In production: await _httpClient.PostAsync(notification.WebhookUrl, ...);
        return Result.Success();
    }
}