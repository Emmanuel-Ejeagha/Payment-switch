using BuildingBlocks.Shared.Paging;

namespace Payment.Application.Features.Queries.ListCustomersByMerchant;

public record ListCustomersByMerchantQuery(Guid MerchantId, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);
