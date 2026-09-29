using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Tests;

public class UserPasswordResetTests
{
    [Fact]
    public void InitiatePasswordReset_ShouldStoreTokenHashAndExpiry()
    {
        var user = CreateUser();
        var expires = DateTime.UtcNow.AddHours(1);

        user.InitiatePasswordReset("reset-hash", expires);

        Assert.Equal("reset-hash", user.PasswordResetTokenHash);
        Assert.Equal(expires, user.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public void ResetPassword_ValidToken_ShouldSetNewHash_AndClearToken_AndRevokeSessions()
    {
        var user = CreateUser();
        user.AddRefreshToken("token1", DateTime.UtcNow.AddDays(1));
        user.AddRefreshToken("token2", DateTime.UtcNow.AddDays(1));
        user.InitiatePasswordReset("reset-hash", DateTime.UtcNow.AddHours(1));

        var result = user.ResetPassword("reset-hash", new PasswordHash("new-hash"));

        Assert.Equal(PasswordResetResult.Success, result);
        Assert.Equal(new PasswordHash("new-hash"), user.PasswordHash);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAt);
        Assert.All(user.RefreshTokens, t => Assert.True(t.IsRevoked));
    }

    [Fact]
    public void ResetPassword_ExpiredToken_ShouldFail_AndKeepPassword()
    {
        var user = CreateUser();
        user.InitiatePasswordReset("reset-hash", DateTime.UtcNow.AddHours(-1));
        var original = user.PasswordHash;

        var result = user.ResetPassword("reset-hash", new PasswordHash("new-hash"));

        Assert.Equal(PasswordResetResult.TokenExpired, result);
        Assert.Equal(original, user.PasswordHash);
        Assert.NotNull(user.PasswordResetTokenHash);
    }

    [Fact]
    public void ResetPassword_InvalidToken_ShouldFail()
    {
        var user = CreateUser();
        user.InitiatePasswordReset("correct-hash", DateTime.UtcNow.AddHours(1));

        var result = user.ResetPassword("wrong-hash", new PasswordHash("new-hash"));

        Assert.Equal(PasswordResetResult.InvalidToken, result);
        Assert.NotNull(user.PasswordResetTokenHash);
    }

    [Fact]
    public void ResetPassword_NoToken_ShouldFail()
    {
        var user = CreateUser();

        var result = user.ResetPassword("anything", new PasswordHash("new-hash"));

        Assert.Equal(PasswordResetResult.NoToken, result);
    }

    [Fact]
    public void ResetPassword_SingleUse_SecondAttemptFails()
    {
        var user = CreateUser();
        user.InitiatePasswordReset("reset-hash", DateTime.UtcNow.AddHours(1));

        var first = user.ResetPassword("reset-hash", new PasswordHash("new-hash"));
        var second = user.ResetPassword("reset-hash", new PasswordHash("another-hash"));

        Assert.Equal(PasswordResetResult.Success, first);
        Assert.Equal(PasswordResetResult.NoToken, second);
        Assert.Equal(new PasswordHash("new-hash"), user.PasswordHash);
    }

    [Fact]
    public void ChangePassword_ShouldUpdateHash_AndRevokeAllSessions()
    {
        var user = CreateUser();
        user.AddRefreshToken("token1", DateTime.UtcNow.AddDays(1));

        user.ChangePassword(new PasswordHash("new-hash"));

        Assert.Equal(new PasswordHash("new-hash"), user.PasswordHash);
        Assert.True(user.RefreshTokens[0].IsRevoked);
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("user@domain.com"), new PasswordHash("hash"), new FullName("John Doe"));
    }
}
