using FluentValidation;

namespace Identity.Application.Commands.Admin;

public class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
    }
}
