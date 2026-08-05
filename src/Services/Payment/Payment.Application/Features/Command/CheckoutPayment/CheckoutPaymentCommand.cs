namespace Payment.Application.Features.Command.CheckoutPayment;

public record CheckoutPaymentCommand(
    string Code,
    string CardToken,
    string IdempotencyKey,
    // The cardholder is present on this call, so the CVC is collected here rather
    // than at tokenize time — it must not outlive the authorization.
    string? SecurityCode = null
);
