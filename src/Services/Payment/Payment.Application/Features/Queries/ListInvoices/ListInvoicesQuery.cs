using BuildingBlocks.Shared.Paging;

namespace Payment.Application.Features.Queries.ListInvoices;

/// <summary>
/// Lists invoices for a merchant, optionally narrowed to a single subscription.
/// </summary>
public record ListInvoicesQuery(Guid MerchantId, Guid? SubscriptionId = null, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake);
