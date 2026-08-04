namespace Payment.Application.Features.Command.ReplayWebhookEvent;

public record ReplayWebhookEventCommand(Guid MerchantId, Guid EventId);
