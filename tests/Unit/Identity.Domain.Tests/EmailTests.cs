using Identity.Domain.ValueObjects;

namespace Identity.Domain.Tests;

public class EmailTests
{
    [Fact]
    public void Constructor_ValidEmail_Creates()
    {
        var email = new Email("test@example.com");
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Constructor_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Email(""));
    }

    [Fact]
    public void Equality_SameCaseInsensitive_ShouldBeEqual()
    {
        var email1 = new Email("TEST@EXAMPLE.COM");
        var email2 = new Email("test@example.com");
        Assert.True(email1 == email2);
    }
}
