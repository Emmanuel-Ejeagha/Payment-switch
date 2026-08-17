using Identity.Domain.DomainEvents;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Constructor_ShouldInitializeUserCorrectly()
    {
        var id = Guid.NewGuid();
        var email = new Email("test@example.com");
        var passwordHash = new PasswordHash("hash");
        var fullName = new FullName("Test User");

        var user = new User(id, email, passwordHash, fullName);

        Assert.Equal(id, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(fullName, user.FullName);
        Assert.True(user.IsActive);
        Assert.Contains("Merchant", user.Roles);
        Assert.Empty(user.RefreshTokens);
        Assert.NotEmpty(user.DomainEvents); // UserRegistered event
        Assert.Single(user.DomainEvents, e => e is UserRegisteredDomainEvent);
    }

    [Fact]
    public void ChangePassword_ShouldUpdateHash()
    {
        var user = CreateUser();
        var newHash = new PasswordHash("newHash");
        user.ChangePassword(newHash);
        Assert.Equal(newHash, user.PasswordHash);
    }

    [Fact]
    public void ActivateDeactivate_ShouldToggleIsActive()
    {
        var user = CreateUser();
        user.Deactivate();
        Assert.False(user.IsActive);
        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void AddRole_ShouldAddIfNotPresent()
    {
        var user = CreateUser();
        user.AddRole("Admin");
        Assert.Contains("Admin", user.Roles);
        user.AddRole("Admin");
        Assert.Single(user.Roles, r => r == "Admin");
    }

    [Fact]
    public void RemoveRole_ShouldRemoveIfPresent()
    {
        var user = CreateUser();
        user.RemoveRole("Merchant");
        Assert.DoesNotContain("Merchant", user.Roles);
    }

    [Fact]
    public void AddRefreshToken_ShouldAddToList()
    {
        var user = CreateUser();
        var token = user.AddRefreshToken("refresh123", DateTime.UtcNow.AddDays(1));
        Assert.Single(user.RefreshTokens);
        Assert.Equal("refresh123", token.Value);
    }

    [Fact]
    public void RevokeRefreshToken_ShouldMarkRevoked()
    {
        var user = CreateUser();
        user.AddRefreshToken("refresh123", DateTime.UtcNow.AddDays(1));
        user.RevokeRefreshToken("refresh123");
        Assert.True(user.RefreshTokens[0].IsRevoked);
    }

    [Fact]
    public void RevokeRefreshToken_UnknownToken_ShouldNotThrow()
    {
        var user = CreateUser();
        user.RevokeRefreshToken("nonexistent");
    }

    [Fact]
    public void RevokeAllRefreshTokens_ShouldRevokeEveryToken()
    {
        var user = CreateUser();
        user.AddRefreshToken("token1", DateTime.UtcNow.AddDays(1));
        user.AddRefreshToken("token2", DateTime.UtcNow.AddDays(1));
        user.AddRefreshToken("token3", DateTime.UtcNow.AddDays(1));

        user.RevokeAllRefreshTokens();

        Assert.All(user.RefreshTokens, t => Assert.True(t.IsRevoked));
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("user@domain.com"), new PasswordHash("hash"), new FullName("John Doe"));
    }
}