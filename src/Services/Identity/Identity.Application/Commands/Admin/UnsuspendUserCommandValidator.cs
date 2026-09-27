using FluentValidation;

namespace Identity.Application.Commands.Admin;

public class UnsuspendUserCommandValidator : AbstractValidator<UnsuspendUserCommand>
{
    public UnsuspendUserCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
    }
}
