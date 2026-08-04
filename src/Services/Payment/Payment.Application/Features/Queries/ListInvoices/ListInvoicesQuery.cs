namespace Payment.Application.Features.Queries.ListInvoices;

/// <summary>
/// Lists invoices for a merchant, optionally narrowed to a single subscription.
/// </summary>
public record ListInvoicesQuery(Guid MerchantId, Guid? SubscriptionId = null, int Skip = 0, int Take = 50);
