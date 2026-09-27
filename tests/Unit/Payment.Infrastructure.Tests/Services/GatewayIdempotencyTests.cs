using Microsoft.Extensions.Logging.Abstractions;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Services.Gateways;

namespace Payment.Infrastructure.Tests.Services;

public class GatewayIdempotencyTests
{
    private static ResilientPaymentGatewayService CreateService()
    {
        var providers = new IPaymentGatewayProvider[] { new StripeMockGatewayProvider() };
        return new ResilientPaymentGatewayService(
            new GatewayProviderRegistry(providers),
            new GatewayRouter(),
            NullLogger<ResilientPaymentGatewayService>.Instance);
    }

    [Fact]
    public async Task CaptureAsync_SameKeyTwice_ReturnsIdenticalReference()
    {
        var service = CreateService();
        var merchantId = Guid.NewGuid();
        var gatewayRef = new GatewayReference("GW-1");

        var first = await service.CaptureAsync(merchantId, gatewayRef, new Money(100, "USD"), "key-1");
        var second = await service.CaptureAsync(merchantId, gatewayRef, new Money(100, "USD"), "key-1");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.GatewayReference, second.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_DifferentKeys_ExecutesTwice()
    {
        var service = CreateService();
        var merchantId = Guid.NewGuid();
        var gatewayRef = new GatewayReference("GW-1");

        var first = await service.CaptureAsync(merchantId, gatewayRef, new Money(100, "USD"), "key-a");
        var second = await service.CaptureAsync(merchantId, gatewayRef, new Money(100, "USD"), "key-b");

        Assert.NotEqual(first.Value!.GatewayReference, second.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_NullKey_ExecutesEveryTime()
    {
        var service = CreateService();
        var merchantId = Guid.NewGuid();
        var gatewayRef = new GatewayReference("GW-1");

        var first = await service.CaptureAsync(merchantId, gatewayRef, new Money(100, "USD"));
        var second = await service.CaptureAsync(merchantId, gatewayRef, new Money(100, "USD"));

        Assert.NotEqual(first.Value!.GatewayReference, second.Value!.GatewayReference);
    }

    [Fact]
    public async Task AuthorizeAsync_SameKeyTwice_ReturnsIdenticalReferences()
    {
        var service = CreateService();

        var first = await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), null, null, "auth-key-1");
        var second = await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), null, null, "auth-key-1");

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value!.GatewayReference, second.Value!.GatewayReference);
        Assert.Equal(first.Value.AuthorizationCode, second.Value.AuthorizationCode);
    }
}
