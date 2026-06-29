using FluentValidation;

namespace Payment.Application.Features.Command.VoidPayment;

public class VoidPaymentCommandValidator : AbstractValidator<VoidPaymentCommand>
{
    public VoidPaymentCommandValidator()
    {
        RuleFor(x => x.IntentId).NotEmpty();
    }
}
