using BuildingBlocks.Shared.Paging;

namespace Payment.Application.Features.Queries.ListWebhookEvents;

public record ListWebhookEventsQuery(Guid MerchantId, int Skip = PageBounds.DefaultSkip, int Take = PageBounds.DefaultTake, Auth.CallerContext? Caller = null);
