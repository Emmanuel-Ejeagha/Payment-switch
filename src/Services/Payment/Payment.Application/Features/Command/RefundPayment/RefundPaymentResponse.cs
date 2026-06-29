namespace Payment.Application.Features.Command.RefundPayment;

public record RefundPaymentResponse(Guid TransactionId, string Status);
