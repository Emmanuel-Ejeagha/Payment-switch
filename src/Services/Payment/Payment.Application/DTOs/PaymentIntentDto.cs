namespace Payment.Application.DTOs;

public record PaymentIntentDto(
    Guid IntentId,
    Guid MerchantId,
    decimal Amount,
    string Currency,
    string Status,
    List<TransactionDto> Transactions
);