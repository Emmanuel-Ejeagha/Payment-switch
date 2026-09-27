using BuildingBlocks.Shared.Security;
using FluentValidation;

namespace Identity.Application.Commands.Auth.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"Password must be at least {PasswordPolicy.MinLength} characters.")
            .MaximumLength(PasswordPolicy.MaxLength)
            .Must(PasswordPolicy.HasComplexity)
            .WithMessage("Password must contain a letter, a digit, and an uppercase letter or symbol.")
            .Must(p => !PasswordPolicy.IsCommonPassword(p))
            .WithMessage("This password is too common. Choose a less predictable password.");

        RuleFor(x => x).Must(x => x.NewPassword != x.CurrentPassword)
            .WithMessage("New password must be different from the current password.")
            .WithName("NewPassword");
    }
}
