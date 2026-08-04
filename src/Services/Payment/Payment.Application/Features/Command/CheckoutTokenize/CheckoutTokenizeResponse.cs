namespace Payment.Application.Features.Command.CheckoutTokenize;

public record CheckoutTokenizeResponse(string Token, string LastFour, string Brand);
