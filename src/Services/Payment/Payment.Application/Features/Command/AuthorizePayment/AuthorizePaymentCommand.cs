namespace Payment.Application.Features.Command.AuthorizePayment;

public record AuthorizePaymentCommand(
    Guid IntentId,
    string? CardLastFour,
    string? CardBrand
);