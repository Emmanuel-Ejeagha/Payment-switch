using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Features.Commands.RunReconciliation;
using Ledger.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Queries.GetLatestReconciliation;

public class GetLatestReconciliationQuery
{
}

public class GetLatestReconciliationHandler
{
    private readonly IReconciliationReportRepository _reports;
    private readonly ILogger<GetLatestReconciliationHandler> _logger;

    public GetLatestReconciliationHandler(IReconciliationReportRepository reports, ILogger<GetLatestReconciliationHandler> logger)
    {
        _reports = reports;
        _logger = logger;
    }

    public async Task<Result<ReconciliationReportDto?>> Handle(
        GetLatestReconciliationQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName}", nameof(GetLatestReconciliationQuery));
        var report = await _reports.GetLatestAsync(cancellationToken);
        return report is null ? null : RunReconciliationHandler.Map(report);
    }
}