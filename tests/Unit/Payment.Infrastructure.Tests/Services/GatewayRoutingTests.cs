using Microsoft.Extensions.Logging.Abstractions;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Services.Gateways;

namespace Payment.Infrastructure.Tests.Services;

public class GatewayRoutingTests
{
    private static ResilientPaymentGatewayService CreateService()
    {
        var providers = new IPaymentGatewayProvider[]
        {
            new PaystackMockGatewayProvider(),
            new StripeMockGatewayProvider()
        };
        return new ResilientPaymentGatewayService(
            new GatewayProviderRegistry(providers),
            new GatewayRouter(),
            NullLogger<ResilientPaymentGatewayService>.Instance);
    }

    [Fact]
    public async Task CaptureAsync_KwdBelowMajorThreshold_RoutesToPaystack()
    {
        // 100_000 minor = KWD 100.000 < KWD 500: raw minor-unit comparison
        // (100000 >= 500_00) used to misroute this to stripe.
        var service = CreateService();

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(100_000, "KWD"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("paystack-", result.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_UsdAboveMajorThreshold_RoutesToStripe()
    {
        var service = CreateService();

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(50_000, "USD"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("stripe-", result.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_JpyAboveMajorThreshold_RoutesToStripe()
    {
        var service = CreateService();

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(50_000, "JPY"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("stripe-", result.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_BelowThresholdNoCard_RoutesToPaystack()
    {
        var service = CreateService();

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(49_999, "USD"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("paystack-", result.Value!.GatewayReference);
    }
}
