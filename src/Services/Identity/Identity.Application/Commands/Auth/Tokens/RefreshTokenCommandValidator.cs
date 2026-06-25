using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Commands.Auth.Tokens;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}