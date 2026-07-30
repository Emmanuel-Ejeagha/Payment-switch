using System.Text;
using System.Text.Json;
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
        if (string.IsNullOrWhiteSpace(notification.WebhookUrl))
        {
            _logger.LogWarning("WEBHOOK SKIPPED: No webhook URL for notification {NotificationId}", notification.Id);
            return new Error("Webhook.NoUrl", "No webhook URL configured.");
        }

        try
        {
            var payload = string.IsNullOrWhiteSpace(notification.Payload)
                ? "{}"
                : notification.Payload;

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var response = await _httpClient.PostAsync(notification.WebhookUrl, content, cts.Token);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WEBHOOK SENT: URL={Url}, StatusCode={StatusCode}",
                    notification.WebhookUrl, (int)response.StatusCode);
                return Result.Success();
            }

            _logger.LogWarning("WEBHOOK FAILED: URL={Url}, StatusCode={StatusCode}, Body={Body}",
                notification.WebhookUrl, (int)response.StatusCode, responseBody);
            return new Error("Webhook.Failed", $"Webhook returned {(int)response.StatusCode}: {responseBody}");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("WEBHOOK TIMEOUT: URL={Url}", notification.WebhookUrl);
            return new Error("Webhook.Timeout", "Webhook request timed out after 15 seconds.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WEBHOOK ERROR: URL={Url}", notification.WebhookUrl);
            return new Error("Webhook.Error", $"Webhook request failed: {ex.Message}");
        }
    }
}
