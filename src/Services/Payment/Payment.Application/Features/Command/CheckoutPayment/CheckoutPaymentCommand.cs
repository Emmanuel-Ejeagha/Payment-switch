namespace Payment.Application.Features.Command.CheckoutPayment;

public record CheckoutPaymentCommand(
    string Code,
    string CardToken,
    string IdempotencyKey
);
