using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Commands.ApiKey;

public class RevokeApiKeyCommandValidator : AbstractValidator<RevokeApiKeyCommand>
{
    public RevokeApiKeyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.KeyId).NotEmpty();
    }
}
