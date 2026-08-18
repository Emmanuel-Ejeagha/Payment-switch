using Payment.Domain.ValueObjects;

namespace Payment.Domain.Tests;

public class IdempotencyKeyTests
{
    [Theory]
    [InlineData("idem-1", true)]
    [InlineData("aBc_123-XYZ", true)]
    [InlineData("f6a41bcad6d041d7a4f6d85d2a2e0d50", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("key with spaces!", false)]
    [InlineData("key#hash", false)]
    [InlineData("key,comma", false)]
    public void IsWellFormed_ShouldValidateFormat(string? key, bool expected)
    {
        Assert.Equal(expected, IdempotencyKey.IsWellFormed(key));
    }

    [Fact]
    public void IsWellFormed_Null_ReturnsFalse()
    {
        Assert.False(IdempotencyKey.IsWellFormed(null));
    }

    [Fact]
    public void IsWellFormed_AtMaxLength_ReturnsTrue()
    {
        Assert.True(IdempotencyKey.IsWellFormed(new string('a', IdempotencyKey.MaxLength)));
    }

    [Fact]
    public void IsWellFormed_OverMaxLength_ReturnsFalse()
    {
        Assert.False(IdempotencyKey.IsWellFormed(new string('a', IdempotencyKey.MaxLength + 1)));
    }

    [Fact]
    public void Constructor_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new IdempotencyKey(""));
    }
}