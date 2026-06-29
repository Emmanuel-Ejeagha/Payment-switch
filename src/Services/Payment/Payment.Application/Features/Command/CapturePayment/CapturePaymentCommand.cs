namespace Payment.Application.Features.Command.CapturePayment;

public record CapturePaymentCommand(
    Guid IntentId,
    decimal? Amount
);