namespace Payment.Application.Features.Command.RefundPayment;

public record RefundPaymentCommand(
    Guid IntentId,
    decimal? Amount
);
