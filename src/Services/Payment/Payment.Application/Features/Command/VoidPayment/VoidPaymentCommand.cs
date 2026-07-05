namespace Payment.Application.Features.Command.VoidPayment;

public record VoidPaymentCommand(Guid IntentId);
