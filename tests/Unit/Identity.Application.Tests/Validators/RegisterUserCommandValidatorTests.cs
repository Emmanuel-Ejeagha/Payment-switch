using BuildingBlocks.Shared.Security;
using Identity.Application.Commands.Auth.Register;

namespace Identity.Application.Tests.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new RegisterUserCommand("test@example.com", "Morningcoffee1", "John Doe"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("123456789")]
    [InlineData("abcdefghij")]
    public void InvalidPassword_Fails(string password)
    {
        var result = _validator.Validate(new RegisterUserCommand("test@example.com", password, "John Doe"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public void OverMaxLengthPassword_Fails()
    {
        var password = "a1" + new string('x', PasswordPolicy.MaxLength);
        var result = _validator.Validate(new RegisterUserCommand("test@example.com", password, "John Doe"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public void MissingEmail_Fails()
    {
        var result = _validator.Validate(new RegisterUserCommand("", "Morningcoffee1", "John Doe"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}