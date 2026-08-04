namespace Payment.Application.Features.Queries.ListPaymentLinksByMerchant;

public record ListPaymentLinksByMerchantQuery(Guid MerchantId, int Skip = 0, int Take = 20);
