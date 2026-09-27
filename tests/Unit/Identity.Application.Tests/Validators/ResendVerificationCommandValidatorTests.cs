using Identity.Application.Commands.Auth.ResendVerification;

namespace Identity.Application.Tests.Validators;

public class ResendVerificationCommandValidatorTests
{
    private readonly ResendVerificationCommandValidator _validator = new();

    [Fact]
    public void ValidEmail_Passes()
    {
        var result = _validator.Validate(new ResendVerificationCommand("test@example.com"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var result = _validator.Validate(new ResendVerificationCommand(""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var result = _validator.Validate(new ResendVerificationCommand("not-an-email"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}