using BuildingBlocks.Shared.Security;

namespace BuildingBlocks.Shared.Tests;

public class DataMaskerTests
{
    [Theory]
    [InlineData("john@example.com", "j***n@example.com")]
    [InlineData("a@example.com", "a***@example.com")]
    [InlineData("ab@example.com", "a***b@example.com")]
    [InlineData(null, "***")]
    [InlineData("not-an-email", "***")]
    public void MaskEmail_ShouldMaskLocalPart(string? email, string expected)
    {
        Assert.Equal(expected, DataMasker.MaskEmail(email));
    }

    [Theory]
    [InlineData("https://hooks.example.com/cb?token=secret123", "https://hooks.example.com:443/cb")]
    [InlineData("https://example.com", "https://example.com:443/")]
    [InlineData(null, "***")]
    [InlineData("not-a-url", "***")]
    public void MaskUrl_ShouldStripQueryString(string? url, string expected)
    {
        Assert.Equal(expected, DataMasker.MaskUrl(url));
    }
}
