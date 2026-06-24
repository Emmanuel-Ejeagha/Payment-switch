using BuildingBlocks.Shared.Exceptions;

namespace BuildingBlocks.Shared.Tests;

public class CommonExtensionsTests
{
    [Fact]
    public void Guard_AgainstNull_WithNull_ShouldThrowArgumentNullException()
    {
        object? value = null;
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(value, nameof(value)));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Guard_AgainstNull_WithNonNull_ShouldNotThrow()
    {
        var value = new object();
        Guard.AgainstNull(value, nameof(value)); 
    }

    [Fact]
    public void Guard_AgainstNullOrEmpty_WithNull_ShouldThrowArgumentNullException()
    {
        string? value = null;
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.AgainstNullOrEmpty(value, nameof(value)));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Guard_AgainstNullOrEmpty_WithEmpty_ShouldThrow()
    {
        var value = string.Empty;
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(value, nameof(value)));
        Assert.Equal("value", exception.ParamName);
        Assert.Contains("empty", exception.Message);
    }

    [Fact]
    public void Guard_AgainstNullOrEmpty_WithValue_ShouldNotThrow()
    {
        var value = "hello";
        Guard.AgainstNullOrEmpty(value, nameof(value)); // no throw
    }

    [Fact]
    public void StringExtensions_IsEmpty_ShouldReturnTrueForNull()
    {
        string? value = null;
        Assert.True(value.IsEmpty());
    }

    [Fact]
    public void StringExtensions_IsEmpty_ShouldReturnTrueForEmpty()
    {
        var value = string.Empty;
        Assert.True(value.IsEmpty());
    }

    [Fact]
    public void StringExtensions_IsEmpty_ShouldReturnFalseForNonEmpty()
    {
        var value = "test";
        Assert.False(value.IsEmpty());
    }
}