namespace Merchant.Application.Features.Commands.ActivateMerchant;

public class ActivateMerchantCommandValidator : AbstractValidator<ActivateMerchantCommand>
{
    public ActivateMerchantCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
    }
}
