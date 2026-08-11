using FluentValidation;

namespace Identity.Application.Commands.Auth.VerifyEmail;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Token).NotEmpty().WithMessage("Verification token is required.");
    }
}
