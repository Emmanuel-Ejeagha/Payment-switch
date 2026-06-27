using Merchant.Domain;
using Merchant.Domain.ValueObjects;

namespace Merchant.Domain.Tests;

public class ValueObjectTests
{
    [Theory]
    [InlineData("Valid Name")]
    [InlineData("Ab")]
    public void BusinessName_Valid_ShouldNotThrow(string name)
    {
        var exception = Record.Exception(() => new BusinessName(name));
        Assert.Null(exception);
    }

    [Fact]
    public void BusinessName_Invalid_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new BusinessName(""));
    }

    [Fact]
    public void MerchantEmail_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MerchantEmail("invalid"));
    }

    [Fact]
    public void MerchantEmail_Valid_ShouldCreateAndLowercase()
    {
        var email = new MerchantEmail("Test@Example.com");
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void WebhookUrl_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => new WebhookUrl("ftp://test.com"));
    }

    [Fact]
    public void BusinessName_TooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => new BusinessName("A"));
    }
}