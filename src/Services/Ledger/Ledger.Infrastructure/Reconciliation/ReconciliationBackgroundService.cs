using BuildingBlocks.Shared.Configuration;
using Ledger.Application.Features.Commands.RunReconciliation;
using Ledger.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ledger.Infrastructure.Reconciliation;

public class ReconciliationOptions
{
    /// <summary>How often the scheduled reconciliation audit runs.</summary>
    public int IntervalMinutes { get; set; } = 15;
}

/// <summary>
/// Periodic reconciliation job: recomputes every account balance from its journal
/// and persists a report. Mismatches are logged as errors so alerting (Serilog /
/// Grafana) surfaces financial-integrity failures without blocking the API.
/// </summary>
public class ReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ReconciliationOptions> _options;
    private readonly ILogger<ReconciliationBackgroundService> _logger;

    public ReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReconciliationOptions> options,
        ILogger<ReconciliationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(
            _options.Value.IntervalMinutes > 0 ? _options.Value.IntervalMinutes : 15);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconciliation background job failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunReconciliationAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var reconciliation = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        var reports = scope.ServiceProvider.GetRequiredService<IReconciliationReportRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var items = await reconciliation.ComputeAsync(cancellationToken);
        var report = new Ledger.Domain.Entities.ReconciliationReport(Guid.NewGuid(), items, DateTime.UtcNow);

        await reports.AddAsync(report, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (report.MismatchCount > 0)
        {
            _logger.LogError(
                "PERIODIC RECONCILIATION MISMATCH: {MismatchCount} of {TotalAccounts} ledger accounts do not tie out to their journal.",
                report.MismatchCount, report.TotalAccounts);
        }
    }
}