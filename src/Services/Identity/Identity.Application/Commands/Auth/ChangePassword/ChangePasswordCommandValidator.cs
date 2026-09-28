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
            .Must(p => p is not null && p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage("Password must contain at least one letter and one digit.");

        RuleFor(x => x).Must(x => x.NewPassword != x.CurrentPassword)
            .WithMessage("New password must be different from the current password.")
            .WithName("NewPassword");
    }
}
