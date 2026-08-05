namespace Payment.Application.Features.Command.CreatePaymentIntent;

public record CreatePaymentIntentCommand(
    Guid MerchantId,
    long Amount,
    string Currency,
    string PaymentMethod,
    string? CardLastFour,
    string? CardBrand,
    string IdempotencyKey,
    string? CardToken = null,
    // Transient CVC for this authorization only. Forwarded to the gateway and then
    // dropped — it is never written to the PaymentIntent or any other table.
    string? SecurityCode = null
);
