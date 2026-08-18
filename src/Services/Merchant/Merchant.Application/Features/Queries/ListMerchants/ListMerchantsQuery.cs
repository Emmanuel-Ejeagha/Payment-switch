using BuildingBlocks.Shared.Paging;

namespace Merchant.Application.Features.Queries.ListMerchants;

public record ListMerchantsQuery(int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);
