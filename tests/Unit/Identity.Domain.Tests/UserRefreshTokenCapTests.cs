using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Tests;

public class UserRefreshTokenCapTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void EnforceRefreshTokenCap_UnderCap_DoesNotRevoke()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxActiveRefreshTokens - 1; i++)
            user.AddRefreshToken($"token-{i}", Now.AddDays(i + 1));

        user.EnforceRefreshTokenCap();

        Assert.DoesNotContain(user.RefreshTokens, t => t.IsRevoked);
    }

    [Fact]
    public void EnforceRefreshTokenCap_OverCap_RevokesOldestOnly()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxActiveRefreshTokens + 3; i++)
            user.AddRefreshToken($"token-{i}", Now.AddDays(i + 1));

        user.EnforceRefreshTokenCap();

        // Exactly the 3 oldest tokens are revoked.
        Assert.Equal(3, user.RefreshTokens.Count(t => t.IsRevoked));
        Assert.True(user.RefreshTokens.First(t => t.Value == "token-0").IsRevoked);
        Assert.True(user.RefreshTokens.First(t => t.Value == "token-2").IsRevoked);
        Assert.False(user.RefreshTokens.First(t => t.Value == "token-3").IsRevoked);
    }

    [Fact]
    public void EnforceRefreshTokenCap_IgnoresRevokedAndExpiredTokens()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxActiveRefreshTokens; i++)
            user.AddRefreshToken($"active-{i}", Now.AddDays(i + 1));
        user.AddRefreshToken("expired", Now.AddDays(-1));
        user.AddRefreshToken("revoked", Now.AddDays(1));
        user.RevokeRefreshToken("revoked");

        user.EnforceRefreshTokenCap();

        Assert.Equal(0, user.RefreshTokens.Count(t => t.IsRevoked && t.Value != "revoked"));
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("user@domain.com"), new PasswordHash("hash"), new FullName("John Doe"));
    }
}
