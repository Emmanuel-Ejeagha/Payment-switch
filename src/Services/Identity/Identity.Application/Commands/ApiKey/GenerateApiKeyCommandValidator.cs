using FluentValidation;

namespace Identity.Application.Commands.ApiKey;

public class GenerateApiKeyCommandValidator : AbstractValidator<GenerateApiKeyCommand>
{
    public GenerateApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Environment)
            .NotEmpty()
            .Must(e => e == "live" || e == "test")
            .WithMessage("Environment must be 'live' or 'test'.");
    }
}
