namespace Payment.Application.Features.Command.AuthorizePayment;

public record AuthorizePaymentResponse(string AuthorizationCode, string GatewayReference, string Status);