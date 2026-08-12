namespace Merchant.Application.Features.Commands.ApproveMerchant;

public class ApproveMerchantCommandValidator : AbstractValidator<ApproveMerchantCommand>
{
    public ApproveMerchantCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
    }
}