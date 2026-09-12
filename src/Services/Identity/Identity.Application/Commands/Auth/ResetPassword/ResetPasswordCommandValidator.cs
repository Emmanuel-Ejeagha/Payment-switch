using BuildingBlocks.Shared.Security;
using FluentValidation;

namespace Identity.Application.Commands.Auth.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255);

        RuleFor(x => x.Token).NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters.")
            .MaximumLength(PasswordPolicy.MaxLength)
            .Must(PasswordPolicy.HasComplexity)
            .WithMessage("Password must contain a letter, a digit, and an uppercase letter or symbol.")
            .Must(p => !PasswordPolicy.IsCommonPassword(p))
            .WithMessage("This password is too common. Choose a less predictable password.");
    }
}
