namespace Merchant.Application.Features.Commands.SuspendMerchant;

public class SuspendMerchantCommandValidator : AbstractValidator<SuspendMerchantCommand>
{
    public SuspendMerchantCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
    }
}
