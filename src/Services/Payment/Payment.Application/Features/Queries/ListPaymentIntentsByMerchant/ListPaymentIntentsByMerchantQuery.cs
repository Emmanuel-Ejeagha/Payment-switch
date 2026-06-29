namespace Payment.Application.Features.Queries.ListPaymentIntentsByMerchant;

public record ListPaymentIntentsByMerchantQuery(Guid MerchantId, int Skip, int Take);
