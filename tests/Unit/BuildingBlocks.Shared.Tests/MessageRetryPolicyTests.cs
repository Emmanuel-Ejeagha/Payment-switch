using BuildingBlocks.Shared.Messaging;

namespace BuildingBlocks.Shared.Tests;

public class MessageRetryPolicyTests
{
    [Fact]
    public void GetRetryCount_WhenNoHeaders_ReturnsZero()
    {
        Assert.Equal(0, MessageRetryPolicy.GetRetryCount(null));
        Assert.Equal(0, MessageRetryPolicy.GetRetryCount(new Dictionary<string, object?>()));
    }

    [Fact]
    public void GetRetryCount_WhenHeaderAbsent_ReturnsZero()
    {
        var headers = new Dictionary<string, object?> { ["other"] = 5L };
        Assert.Equal(0, MessageRetryPolicy.GetRetryCount(headers));
    }

    [Fact]
    public void GetRetryCount_WhenLongHeader_ReturnsValue()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = 2L };
        Assert.Equal(2, MessageRetryPolicy.GetRetryCount(headers));
    }

    [Fact]
    public void GetRetryCount_WhenIntHeader_ReturnsValue()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = 1 };
        Assert.Equal(1, MessageRetryPolicy.GetRetryCount(headers));
    }

    [Fact]
    public void GetRetryCount_WhenByteArrayHeader_ReturnsValue()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = System.Text.Encoding.UTF8.GetBytes("3") };
        Assert.Equal(3, MessageRetryPolicy.GetRetryCount(headers));
    }

    [Fact]
    public void GetRetryCount_WhenStringHeader_ReturnsValue()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = "2" };
        Assert.Equal(2, MessageRetryPolicy.GetRetryCount(headers));
    }

    [Fact]
    public void GetRetryCount_WhenUnparseable_ReturnsZero()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = "abc" };
        Assert.Equal(0, MessageRetryPolicy.GetRetryCount(headers));
    }

    [Fact]
    public void ShouldRetry_WhenBelowMax_ReturnsTrue()
    {
        Assert.True(MessageRetryPolicy.ShouldRetry(0));
        Assert.True(MessageRetryPolicy.ShouldRetry(2));
    }

    [Fact]
    public void ShouldRetry_WhenAtOrAboveMax_ReturnsFalse()
    {
        Assert.False(MessageRetryPolicy.ShouldRetry(3));
        Assert.False(MessageRetryPolicy.ShouldRetry(4));
    }
}
