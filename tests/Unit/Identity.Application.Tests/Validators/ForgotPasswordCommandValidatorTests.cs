using Identity.Application.Commands.Auth.ForgotPassword;

namespace Identity.Application.Tests.Validators;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidEmail_Passes()
    {
        var result = _validator.Validate(new ForgotPasswordCommand("test@example.com"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var result = _validator.Validate(new ForgotPasswordCommand(""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var result = _validator.Validate(new ForgotPasswordCommand("not-an-email"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmailLongerThan255_Fails()
    {
        var result = _validator.Validate(new ForgotPasswordCommand(new string('a', 260) + "@example.com"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}