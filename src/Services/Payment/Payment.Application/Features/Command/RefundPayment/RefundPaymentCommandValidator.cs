using FluentValidation;

namespace Payment.Application.Features.Command.RefundPayment;

public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.IntentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue)
            .WithMessage("Refund amount must be greater than zero.");
    }
}
