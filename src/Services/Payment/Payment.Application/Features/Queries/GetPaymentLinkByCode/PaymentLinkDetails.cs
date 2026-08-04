namespace Payment.Application.Features.Queries.GetPaymentLinkByCode;

public record PaymentLinkDetails(
    Guid Id,
    long Amount,
    string Currency,
    string Code,
    string Description,
    bool Active
);
