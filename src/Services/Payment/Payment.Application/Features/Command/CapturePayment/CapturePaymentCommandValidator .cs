using FluentValidation;

namespace Payment.Application.Features.Command.CapturePayment;

public class CapturePaymentCommandValidator : AbstractValidator<CapturePaymentCommand>
{
    public CapturePaymentCommandValidator()
    {
        RuleFor(x => x.IntentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue)
            .WithMessage("Capture amount must be greater than zero.");
    }
}