using Identity.Infrastructure.Services;

namespace Identity.Infrastructure.Tests.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnValidHash()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Secret123");
        Assert.NotNull(hash);
        Assert.NotEmpty(hash.Hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ShouldReturnTrue()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Secret123");
        Assert.True(hasher.Verify("Secret123", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ShouldReturnFalse()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("Secret123");
        Assert.False(hasher.Verify("WrongPass", hash));
    }
}