using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Features.Commands.RunReconciliation;
using Ledger.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Queries.ListReconciliationReports;

public class ListReconciliationReportsQuery
{
    public int Skip { get; }
    public int Take { get; }

    public ListReconciliationReportsQuery(int skip = 0, int take = 20)
    {
        Skip = skip < 0 ? 0 : skip;
        Take = take < 1 || take > 100 ? 20 : take;
    }
}

public class ListReconciliationReportsHandler
{
    private readonly IReconciliationReportRepository _reports;
    private readonly ILogger<ListReconciliationReportsHandler> _logger;

    public ListReconciliationReportsHandler(IReconciliationReportRepository reports, ILogger<ListReconciliationReportsHandler> logger)
    {
        _reports = reports;
        _logger = logger;
    }

    public async Task<Result<List<ReconciliationReportDto>>> Handle(
        ListReconciliationReportsQuery query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName}", nameof(ListReconciliationReportsQuery));
        var reports = await _reports.ListAsync(query.Skip, query.Take, cancellationToken);
        return reports.Select(RunReconciliationHandler.Map).ToList();
    }
}