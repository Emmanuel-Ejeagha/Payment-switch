namespace Payment.Application.Features.Command.CreateCardToken;

public record CreateCardTokenCommand(
    Guid MerchantId,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear
);
