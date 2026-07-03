using BuildingBlocks.Shared.Results;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;
using Settlement.Domain.DomainErrors;
using Settlement.Domain.Entities;

namespace Settlement.Application.Features.Queries.GetSettlementBatch;

public class GetSettlementBatchHandler
{
    private readonly ISettlementBatchRepository _repository;

    public GetSettlementBatchHandler(ISettlementBatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SettlementBatchDto>> Handle(GetSettlementBatchQuery query, CancellationToken cancellationToken = default)
    {
        var batch = await _repository.GetByIdAsync(query.BatchId, cancellationToken);
        if (batch is null)
            return SettlementErrors.BatchNotFound(query.BatchId);

        return Map(batch);
    }

    private static SettlementBatchDto Map(SettlementBatch batch) =>
        new(
            batch.Id,
            batch.BatchDate,
            batch.Status.Value,
            batch.TotalAmount,
            batch.Payouts.Select(p => new PayoutDto(p.MerchantId, p.GrossVolume.Amount, p.Fees.Amount, p.NetAmount.Amount, p.Currency)).ToList()
        );
}
