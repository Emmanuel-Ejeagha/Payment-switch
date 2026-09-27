using BuildingBlocks.Shared.Paging;

namespace Payment.Application.Features.Queries.ListSubscriptionsByMerchant;

public record ListSubscriptionsByMerchantQuery(Guid MerchantId, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);
