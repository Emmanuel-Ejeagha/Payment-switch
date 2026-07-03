namespace Settlement.Application.Features.Queries.ListSettlementBatches;

public record ListSettlementBatchesQuery(DateTime? From, DateTime? To, int Skip, int Take);

