using Notification.Application.Messaging;

namespace Notification.Application.Tests.Messaging;

public class PaymentEventMapperTests
{
    private const string MerchantEmail = "merchant@acme.com";

    [Fact]
    public void MapAuthorized_UsesResolvedMerchantEmail()
    {
        var e = new PaymentAuthorizedEvent(Guid.NewGuid(), Guid.NewGuid(), new MoneyPayload(1999, "USD"), "AUTH-123", "gw-ref");

        var command = PaymentEventMapper.MapAuthorized(e, MerchantEmail, "{}");

        Assert.Equal(MerchantEmail, command.Recipient);
        Assert.Equal("email", command.Channel);
        Assert.Equal("Payment Authorized", command.Subject);
        Assert.Contains("1999", command.Body);
        Assert.Contains("USD", command.Body);
        Assert.Equal("{}", command.Payload);
    }

    [Fact]
    public void MapCaptured_UsesResolvedMerchantEmail()
    {
        var e = new PaymentCapturedEvent(Guid.NewGuid(), Guid.NewGuid(), new MoneyPayload(5000, "EUR"), Guid.NewGuid());

        var command = PaymentEventMapper.MapCaptured(e, MerchantEmail, "{}");

        Assert.Equal(MerchantEmail, command.Recipient);
        Assert.Equal("email", command.Channel);
        Assert.Equal("Payment Captured", command.Subject);
        Assert.Contains("5000", command.Body);
        Assert.Contains("EUR", command.Body);
    }

    [Fact]
    public void MapRefunded_UsesResolvedMerchantEmail()
    {
        var e = new PaymentRefundedEvent(Guid.NewGuid(), Guid.NewGuid(), new MoneyPayload(750, "GBP"), Guid.NewGuid());

        var command = PaymentEventMapper.MapRefunded(e, MerchantEmail, "{}");

        Assert.Equal(MerchantEmail, command.Recipient);
        Assert.Equal("email", command.Channel);
        Assert.Equal("Payment Refunded", command.Subject);
        Assert.Contains("750", command.Body);
        Assert.Contains("GBP", command.Body);
    }

    [Fact]
    public void Map_NeverUsesPlaceholderRecipient()
    {
        var body = "{}";
        var commands = new[]
        {
            PaymentEventMapper.MapAuthorized(new PaymentAuthorizedEvent(Guid.NewGuid(), Guid.NewGuid(), new MoneyPayload(1, "USD"), "A", "G"), MerchantEmail, body),
            PaymentEventMapper.MapCaptured(new PaymentCapturedEvent(Guid.NewGuid(), Guid.NewGuid(), new MoneyPayload(1, "USD"), Guid.NewGuid()), MerchantEmail, body),
            PaymentEventMapper.MapRefunded(new PaymentRefundedEvent(Guid.NewGuid(), Guid.NewGuid(), new MoneyPayload(1, "USD"), Guid.NewGuid()), MerchantEmail, body)
        };

        foreach (var command in commands)
        {
            Assert.Equal(MerchantEmail, command.Recipient);
            Assert.DoesNotContain("customer@example.com", command.Recipient);
        }
    }
}
