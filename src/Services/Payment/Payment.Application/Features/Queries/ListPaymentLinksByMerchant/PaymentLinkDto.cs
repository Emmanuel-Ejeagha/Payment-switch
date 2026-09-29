namespace Payment.Application.Features.Queries.ListPaymentLinksByMerchant;

public record PaymentLinkDto(
    Guid Id,
    long Amount,
    string Currency,
    string Code,
    string Description,
    bool Active,
    DateTime CreatedAt
);
