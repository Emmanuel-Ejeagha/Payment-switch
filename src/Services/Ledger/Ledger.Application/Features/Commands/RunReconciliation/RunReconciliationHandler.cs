using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Commands.RunReconciliation;

public class RunReconciliationHandler
{
    private readonly IReconciliationService _reconciliationService;
    private readonly IReconciliationReportRepository _reports;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RunReconciliationHandler> _logger;

    public RunReconciliationHandler(
        IReconciliationService reconciliationService,
        IReconciliationReportRepository reports,
        IUnitOfWork unitOfWork,
        ILogger<RunReconciliationHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _reports = reports;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ReconciliationReportDto>> Handle(
        RunReconciliationCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ledger reconciliation run.");

        var items = await _reconciliationService.ComputeAsync(cancellationToken);
        var report = new ReconciliationReport(Guid.NewGuid(), items, DateTime.UtcNow);

        await _reports.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (report.MismatchCount > 0)
        {
            _logger.LogError(
                "Reconciliation MISMATCH: {MismatchCount} of {TotalAccounts} ledger accounts do not tie out to their journal entries.",
                report.MismatchCount, report.TotalAccounts);
        }
        else
        {
            _logger.LogInformation(
                "Reconciliation passed: all {TotalAccounts} ledger accounts tie out to their journal entries.",
                report.TotalAccounts);
        }

        return Map(report);
    }

    internal static ReconciliationReportDto Map(ReconciliationReport report) =>
        new(
            report.Id,
            report.RunAtUtc,
            report.Status,
            report.TotalAccounts,
            report.MismatchCount,
            report.Items
                .Select(i => new ReconciliationLineItemDto(
                    i.MerchantId,
                    i.Currency,
                    i.ExpectedAvailable,
                    i.ActualAvailable,
                    i.ExpectedPending,
                    i.ActualPending,
                    i.ExpectedReserved,
                    i.ActualReserved,
                    i.IsMatch))
                .ToList());
}