namespace Payment.Application.Features.Command.CreatePaymentIntent;

public record CreatePaymentIntentCommand(
    Guid MerchantId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string? CardLastFour,
    string? CardBrand,
    string IdempotencyKey
);
