namespace Payment.Application.Features.Command.CreatePaymentLink;

public record CreatePaymentLinkResponse(
    Guid Id,
    long Amount,
    string Currency,
    string Code,
    string Description,
    bool Active
);
