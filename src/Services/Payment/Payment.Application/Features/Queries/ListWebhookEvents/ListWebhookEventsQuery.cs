namespace Payment.Application.Features.Queries.ListWebhookEvents;

public record ListWebhookEventsQuery(Guid MerchantId, int Skip = 0, int Take = 20);
