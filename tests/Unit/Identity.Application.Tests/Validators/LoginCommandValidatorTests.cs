using Identity.Application.Commands.Auth.Login;

namespace Identity.Application.Tests.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void ValidLogin_Passes()
    {
        var result = _validator.Validate(new LoginCommand("test@example.com", "Morningcoffee1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var result = _validator.Validate(new LoginCommand("", "Morningcoffee1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void MalformedEmail_Fails()
    {
        var result = _validator.Validate(new LoginCommand("not-an-email", "Morningcoffee1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyPassword_Fails()
    {
        var result = _validator.Validate(new LoginCommand("test@example.com", ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }
}