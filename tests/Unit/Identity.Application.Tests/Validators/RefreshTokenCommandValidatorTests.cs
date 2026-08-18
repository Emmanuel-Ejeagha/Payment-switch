using Identity.Application.Commands.Auth.Tokens;

namespace Identity.Application.Tests.Validators;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void NonEmptyToken_Passes()
    {
        var result = _validator.Validate(new RefreshTokenCommand("some-refresh-token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyToken_Fails()
    {
        var result = _validator.Validate(new RefreshTokenCommand(""));

        Assert.Contains(result.Errors, e => e.PropertyName == "RefreshToken");
    }
}