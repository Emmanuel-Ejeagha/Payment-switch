namespace Payment.Application.Features.Queries.ListSubscriptionsByMerchant;

public record ListSubscriptionsByMerchantQuery(Guid MerchantId, int Skip = 0, int Take = 50);
