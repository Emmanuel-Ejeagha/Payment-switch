namespace Payment.Application.DTOs;

public record PaymentIntentDto(
    Guid IntentId,
    Guid MerchantId,
    long Amount,
    string Currency,
    string Status,
    string? CardLastFour,
    string? CardBrand,
    DateTime CreatedAt,
    List<TransactionDto> Transactions
);