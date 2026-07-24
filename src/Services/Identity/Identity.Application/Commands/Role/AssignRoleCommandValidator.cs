using FluentValidation;

namespace Identity.Application.Commands.Role;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    private static readonly string[] AllowedRoles = { "Admin", "Merchant", "Support" };

    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => AllowedRoles.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");
    }
}
