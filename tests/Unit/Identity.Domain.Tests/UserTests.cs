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
        Assert.Empty(user.ApiKeys);
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
    public void GenerateApiKey_ShouldAddKeyAndRaiseEvent()
    {
        var user = CreateUser();
        var keyId = Guid.NewGuid();
        var keyHash = "abc123";
        user.GenerateApiKey(keyId, keyHash, "live");

        Assert.Single(user.ApiKeys);
        Assert.Equal(keyHash, user.ApiKeys[0].KeyHash);
        Assert.Contains(user.DomainEvents, e => e is ApiKeyGeneratedDomainEvent);
    }

    [Fact]
    public void RevokeApiKey_ShouldRevokeAndRaiseEvent()
    {
        var user = CreateUser();
        var keyId = Guid.NewGuid();
        user.GenerateApiKey(keyId, "hash", "test");
        user.ClearDomainEvents();

        user.RevokeApiKey(keyId);
        Assert.NotNull(user.ApiKeys[0].RevokedAt);
        Assert.Single(user.DomainEvents, e => e is ApiKeyRevokedDomainEvent);
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

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("user@domain.com"), new PasswordHash("hash"), new FullName("John Doe"));
    }
}