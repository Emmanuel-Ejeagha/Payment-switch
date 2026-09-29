using Merchant.Application.DTOs;

namespace Merchant.Application.Tests.DTOs;

public class MerchantDtoTests
{
    [Fact]
    public void MerchantDto_DoesNotExposeWebhookSecret()
    {
        Assert.Null(typeof(MerchantDto).GetProperty("WebhookSecret"));
        Assert.Null(typeof(MerchantDto).GetField("WebhookSecret"));
    }
}
