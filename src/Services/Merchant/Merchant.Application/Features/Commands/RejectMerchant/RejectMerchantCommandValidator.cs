namespace Merchant.Application.Features.Commands.RejectMerchant;

public class RejectMerchantCommandValidator : AbstractValidator<RejectMerchantCommand>
{
    public RejectMerchantCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A rejection reason is required.")
            .MaximumLength(500);
    }
}