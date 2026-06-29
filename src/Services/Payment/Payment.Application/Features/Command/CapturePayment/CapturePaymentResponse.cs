namespace Payment.Application.Features.Command.CapturePayment;

public record CapturePaymentResponse(Guid TransactionId, string Status);
