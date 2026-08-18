using BuildingBlocks.Shared.Paging;

namespace Payment.Application.Features.Queries.ListPaymentIntentsByMerchant;

public record ListPaymentIntentsByMerchantQuery(Guid MerchantId, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);
