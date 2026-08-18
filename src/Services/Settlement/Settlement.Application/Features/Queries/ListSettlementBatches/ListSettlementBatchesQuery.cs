using BuildingBlocks.Shared.Paging;

namespace Settlement.Application.Features.Queries.ListSettlementBatches;

public record ListSettlementBatchesQuery(DateTime? From, DateTime? To, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);

