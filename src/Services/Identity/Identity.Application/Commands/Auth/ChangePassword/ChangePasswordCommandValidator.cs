using FluentValidation;

namespace Identity.Application.Commands.Auth.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100);

        RuleFor(x => x).Must(x => x.NewPassword != x.CurrentPassword)
            .WithMessage("New password must be different from the current password.")
            .WithName("NewPassword");
    }
}
