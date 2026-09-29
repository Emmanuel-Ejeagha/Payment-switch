using Identity.Application.Commands.Auth.VerifyEmail;

namespace Identity.Application.Tests.Validators;

public class VerifyEmailCommandValidatorTests
{
    private readonly VerifyEmailCommandValidator _validator = new();

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new VerifyEmailCommand("test@example.com", "some-token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var result = _validator.Validate(new VerifyEmailCommand("", "some-token"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var result = _validator.Validate(new VerifyEmailCommand("not-an-email", "some-token"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyToken_Fails()
    {
        var result = _validator.Validate(new VerifyEmailCommand("test@example.com", ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Token");
    }
}