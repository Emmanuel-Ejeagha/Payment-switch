namespace Merchant.Application.Features.Commands.ReactivateMerchant;

public class ReactivateMerchantCommandValidator : AbstractValidator<ReactivateMerchantCommand>
{
    public ReactivateMerchantCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
    }
}