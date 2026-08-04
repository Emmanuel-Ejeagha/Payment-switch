namespace Payment.Application.Features.Queries.ListCustomersByMerchant;

public record ListCustomersByMerchantQuery(Guid MerchantId, int Skip = 0, int Take = 20);
