using BuildingBlocks.Shared.Auth;
using FluentValidation;

namespace Identity.Application.Commands.Role;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => RoleNames.IsKnown(role))
            .WithMessage($"Role must be one of: {string.Join(", ", RoleNames.All)}.");
    }
}
