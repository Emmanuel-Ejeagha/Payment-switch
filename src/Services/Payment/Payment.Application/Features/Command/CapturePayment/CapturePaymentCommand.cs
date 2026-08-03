namespace Payment.Application.Features.Command.CapturePayment;

public record CapturePaymentCommand(
    Guid IntentId,
    long? Amount,
    string? IdempotencyKey = null
);