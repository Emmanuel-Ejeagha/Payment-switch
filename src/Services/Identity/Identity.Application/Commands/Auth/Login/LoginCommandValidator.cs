using FluentValidation;

namespace Identity.Application.Commands.Auth.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required").EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
