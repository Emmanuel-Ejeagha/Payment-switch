namespace Payment.Application.Features.Command.CheckoutPayment;

public record CheckoutPaymentResponse(Guid IntentId, string Status, string? ClientSecret);
