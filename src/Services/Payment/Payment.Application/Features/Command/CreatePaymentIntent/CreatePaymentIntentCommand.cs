namespace Payment.Application.Features.Command.CreatePaymentIntent;

public record CreatePaymentIntentCommand(
    Guid MerchantId,
    long Amount,
    string Currency,
    string PaymentMethod,
    string? CardLastFour,
    string? CardBrand,
    string IdempotencyKey,
    string? CardToken = null
);
