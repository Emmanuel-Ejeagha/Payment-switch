namespace Payment.Application.Features.Queries.ListPlansByMerchant;

public record ListPlansByMerchantQuery(Guid MerchantId, int Skip = 0, int Take = 50);
