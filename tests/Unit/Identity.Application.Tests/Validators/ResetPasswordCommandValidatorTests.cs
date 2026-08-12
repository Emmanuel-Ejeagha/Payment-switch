using Identity.Application.Commands.Auth.ResetPassword;

namespace Identity.Application.Tests.Validators;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidPassword_Passes()
    {
        var result = _validator.Validate(new ResetPasswordCommand("test@example.com", "token", "Morningcoffee1"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    [InlineData("abcdefghij")]
    public void WeakPassword_Fails(string password)
    {
        var result = _validator.Validate(new ResetPasswordCommand("test@example.com", "token", password));

        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void MissingToken_Fails()
    {
        var result = _validator.Validate(new ResetPasswordCommand("test@example.com", "", "Morningcoffee1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Token");
    }
}