using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Tests;

public class UserEmailVerificationTests
{
    private static User CreateUser() =>
        new(Guid.NewGuid(), new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));

    [Fact]
    public void NewUser_IsNotEmailConfirmed()
    {
        var user = CreateUser();

        Assert.False(user.EmailConfirmed);
        Assert.Null(user.EmailVerificationTokenHash);
        Assert.Null(user.EmailVerifiedAt);
    }

    [Fact]
    public void VerifyEmail_WithValidToken_ConfirmsAndClearsToken()
    {
        var user = CreateUser();
        user.InitiateEmailVerification("hash", DateTime.UtcNow.AddHours(24));

        var result = user.VerifyEmail("hash");

        Assert.Equal(EmailVerificationResult.Success, result);
        Assert.True(user.EmailConfirmed);
        Assert.NotNull(user.EmailVerifiedAt);
        Assert.Null(user.EmailVerificationTokenHash);
        Assert.Null(user.EmailVerificationTokenExpiresAt);
    }

    [Fact]
    public void VerifyEmail_TokenIsSingleUse()
    {
        var user = CreateUser();
        user.InitiateEmailVerification("hash", DateTime.UtcNow.AddHours(24));

        Assert.Equal(EmailVerificationResult.Success, user.VerifyEmail("hash"));
        var second = user.VerifyEmail("hash");

        Assert.Equal(EmailVerificationResult.AlreadyConfirmed, second);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public void VerifyEmail_WithWrongToken_Fails()
    {
        var user = CreateUser();
        user.InitiateEmailVerification("hash", DateTime.UtcNow.AddHours(24));

        var result = user.VerifyEmail("wrong");

        Assert.Equal(EmailVerificationResult.InvalidToken, result);
        Assert.False(user.EmailConfirmed);
        Assert.Equal("hash", user.EmailVerificationTokenHash);
    }

    [Fact]
    public void VerifyEmail_WithExpiredToken_Fails()
    {
        var user = CreateUser();
        user.InitiateEmailVerification("hash", DateTime.UtcNow.AddHours(-1));

        var result = user.VerifyEmail("hash");

        Assert.Equal(EmailVerificationResult.TokenExpired, result);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public void VerifyEmail_WithoutToken_Fails()
    {
        var user = CreateUser();

        Assert.Equal(EmailVerificationResult.NoToken, user.VerifyEmail("anything"));
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public void VerifyEmail_AlreadyConfirmed_ReturnsAlreadyConfirmed()
    {
        var user = CreateUser();
        user.MarkEmailConfirmed();

        Assert.Equal(EmailVerificationResult.AlreadyConfirmed, user.VerifyEmail("hash"));
    }

    [Fact]
    public void InitiateEmailVerification_ReplacesPreviousToken()
    {
        var user = CreateUser();
        user.InitiateEmailVerification("old-hash", DateTime.UtcNow.AddHours(24));
        user.InitiateEmailVerification("new-hash", DateTime.UtcNow.AddHours(24));

        Assert.Equal("new-hash", user.EmailVerificationTokenHash);
        Assert.Equal(EmailVerificationResult.InvalidToken, user.VerifyEmail("old-hash"));
        Assert.Equal(EmailVerificationResult.Success, user.VerifyEmail("new-hash"));
    }

    [Fact]
    public void InitiateEmailVerification_OnConfirmedUser_Throws()
    {
        var user = CreateUser();
        user.MarkEmailConfirmed();

        Assert.Throws<InvalidOperationException>(() => user.InitiateEmailVerification("hash", DateTime.UtcNow.AddHours(24)));
    }
}
