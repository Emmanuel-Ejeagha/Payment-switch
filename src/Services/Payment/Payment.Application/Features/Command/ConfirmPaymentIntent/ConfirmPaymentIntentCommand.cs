namespace Payment.Application.Features.Command.ConfirmPaymentIntent;

public record ConfirmPaymentIntentCommand(Guid MerchantId, Guid IntentId, string IdempotencyKey);
