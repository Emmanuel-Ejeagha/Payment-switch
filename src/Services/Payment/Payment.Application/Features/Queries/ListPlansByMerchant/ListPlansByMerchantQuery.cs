using BuildingBlocks.Shared.Paging;

namespace Payment.Application.Features.Queries.ListPlansByMerchant;

public record ListPlansByMerchantQuery(Guid MerchantId, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);
