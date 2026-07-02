using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.Features.Commands.SendPendingNotification;
using Notification.Application.Interfaces;

namespace Notification.Infrastructure.Services;

public class NotificationSenderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationSenderBackgroundService> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public NotificationSenderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<NotificationSenderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending notifications");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingNotifications(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var sendHandler = scope.ServiceProvider.GetRequiredService<SendPendingNotificationHandler>();

        var pending = await repository.GetPendingForRetryAsync(DateTime.UtcNow, 10, cancellationToken);

        foreach (var notification in pending)
        {
            try
            {
                await sendHandler.Handle(new SendPendingNotificationCommand(notification.Id), cancellationToken);
                _logger.LogInformation("Processed notification {NotificationId}", notification.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification {NotificationId}", notification.Id);
            }
        }
    }
}