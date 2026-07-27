namespace Payment.Application.DTOs;

public record PaymentIntentDto(
    Guid IntentId,
    Guid MerchantId,
    decimal Amount,
    string Currency,
    string Status,
    string? CardLastFour,
    string? CardBrand,
    List<TransactionDto> Transactions
);