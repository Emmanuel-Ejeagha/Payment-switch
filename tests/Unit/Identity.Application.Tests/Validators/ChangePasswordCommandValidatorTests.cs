using Identity.Application.Commands.Auth.ChangePassword;

namespace Identity.Application.Tests.Validators;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void ValidPassword_Passes()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Oldpassword1", "Morningcoffee1"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    [InlineData("abcdefghij")]
    public void WeakPassword_Fails(string password)
    {
        var result = _validator.Validate(new ChangePasswordCommand("Oldpassword1", password));

        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword");
    }

    [Fact]
    public void MissingCurrentPassword_Fails()
    {
        var result = _validator.Validate(new ChangePasswordCommand("", "Morningcoffee1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "CurrentPassword");
    }

    [Fact]
    public void SameAsCurrentPassword_Fails()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Morningcoffee1", "Morningcoffee1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "NewPassword");
    }
}