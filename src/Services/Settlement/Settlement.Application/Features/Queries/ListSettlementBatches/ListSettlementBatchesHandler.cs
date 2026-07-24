using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;

namespace Settlement.Application.Features.Queries.ListSettlementBatches;

public class ListSettlementBatchesHandler
{
    private readonly ISettlementBatchRepository _repository;
    private readonly ILogger<ListSettlementBatchesHandler> _logger;

    public ListSettlementBatchesHandler(ISettlementBatchRepository repository, ILogger<ListSettlementBatchesHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<SettlementBatchDto>>> Handle(ListSettlementBatchesQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName}", nameof(ListSettlementBatchesQuery));

        var batches = await _repository.ListAsync(query.From, query.To, query.Skip, query.Take, cancellationToken);
        return batches;
    }
}