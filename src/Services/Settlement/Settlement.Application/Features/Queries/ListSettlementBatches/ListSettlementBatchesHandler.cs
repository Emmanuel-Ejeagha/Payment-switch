using BuildingBlocks.Shared.Results;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;

namespace Settlement.Application.Features.Queries.ListSettlementBatches;

public class ListSettlementBatchesHandler
{
    private readonly ISettlementBatchRepository _repository;

    public ListSettlementBatchesHandler(ISettlementBatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<SettlementBatchDto>>> Handle(ListSettlementBatchesQuery query, CancellationToken cancellationToken = default)
    {
        var batches = await _repository.ListAsync(query.From, query.To, query.Skip, query.Take, cancellationToken);
        return batches;
    }
}