namespace Payment.Application.Features.Command.CreatePaymentLink;

public record CreatePaymentLinkCommand(
    Guid MerchantId,
    long Amount,
    string Currency,
    string Description
);
