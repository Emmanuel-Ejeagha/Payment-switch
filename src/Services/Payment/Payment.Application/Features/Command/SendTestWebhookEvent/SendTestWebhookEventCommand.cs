namespace Payment.Application.Features.Command.SendTestWebhookEvent;

public record SendTestWebhookEventCommand(Guid MerchantId, string EventType = "test.event", string? CorrelationId = null, Auth.CallerContext? Caller = null);
