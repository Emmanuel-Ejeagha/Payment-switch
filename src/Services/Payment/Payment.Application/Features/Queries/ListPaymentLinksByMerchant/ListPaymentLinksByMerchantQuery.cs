using BuildingBlocks.Shared.Paging;

namespace Payment.Application.Features.Queries.ListPaymentLinksByMerchant;

public record ListPaymentLinksByMerchantQuery(Guid MerchantId, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);
