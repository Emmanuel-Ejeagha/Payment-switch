using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;
using Settlement.Domain.DomainErrors;
using Settlement.Domain.Entities;

namespace Settlement.Application.Features.Queries.GetSettlementBatch;

public class GetSettlementBatchHandler
{
    private readonly ISettlementBatchRepository _repository;
    private readonly ILogger<GetSettlementBatchHandler> _logger;

    public GetSettlementBatchHandler(ISettlementBatchRepository repository, ILogger<GetSettlementBatchHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<SettlementBatchDto>> Handle(GetSettlementBatchQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName} with Id {Id}", nameof(GetSettlementBatchQuery), query.BatchId);

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
