using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ledger.Infrastructure.Inbox;

public class InboxCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InboxCleanupService> _logger;
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public InboxCleanupService(IServiceScopeFactory scopeFactory, ILogger<InboxCleanupService> logger)
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
                await CleanupProcessedMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up inbox messages");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupProcessedMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - _retentionPeriod;
        var deleted = await db.InboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            _logger.LogInformation("Cleaned up {DeletedCount} processed inbox messages older than {RetentionPeriod}", deleted, _retentionPeriod);
        }
    }
}
