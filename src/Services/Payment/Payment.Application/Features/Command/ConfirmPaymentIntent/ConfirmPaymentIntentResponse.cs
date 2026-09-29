namespace Payment.Application.Features.Command.ConfirmPaymentIntent;

public record ConfirmPaymentIntentResponse(Guid IntentId, string Status, string? ClientSecret);
