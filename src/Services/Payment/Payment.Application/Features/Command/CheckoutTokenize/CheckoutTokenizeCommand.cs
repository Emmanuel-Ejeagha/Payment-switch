namespace Payment.Application.Features.Command.CheckoutTokenize;

public record CheckoutTokenizeCommand(
    string Code,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear
);
