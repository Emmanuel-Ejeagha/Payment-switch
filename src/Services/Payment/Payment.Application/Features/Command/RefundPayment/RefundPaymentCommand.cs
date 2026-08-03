namespace Payment.Application.Features.Command.RefundPayment;

public record RefundPaymentCommand(
    Guid IntentId,
    long? Amount,
    string? IdempotencyKey = null
);
