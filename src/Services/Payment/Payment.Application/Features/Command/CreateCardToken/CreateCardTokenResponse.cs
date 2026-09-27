namespace Payment.Application.Features.Command.CreateCardToken;

public record CreateCardTokenResponse(
    string Token,
    string LastFour,
    string Brand,
    int ExpiryMonth,
    int ExpiryYear
);
