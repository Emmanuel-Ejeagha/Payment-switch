using Microsoft.Extensions.Logging.Abstractions;
using Payment.Application.DTOs;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Services.Gateways;

namespace Payment.Infrastructure.Tests.Services;

public class GatewayPinningTests
{
    private static (ResilientPaymentGatewayService Service, GatewayProviderRegistry Registry) CreateService()
    {
        var providers = new IPaymentGatewayProvider[]
        {
            new PaystackMockGatewayProvider(),
            new StripeMockGatewayProvider()
        };
        var registry = new GatewayProviderRegistry(providers);
        var service = new ResilientPaymentGatewayService(
            registry,
            new GatewayRouter(),
            NullLogger<ResilientPaymentGatewayService>.Instance);
        return (service, registry);
    }

    [Fact]
    public async Task CaptureAsync_PinnedProvider_WinsOverRouterPreference()
    {
        // Small amount with no card routes to paystack by default; pinning to
        // stripe must keep the follow-on on the authorizing acquirer.
        var (service, _) = CreateService();

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(100, "USD"), "key-1", "stripe");

        Assert.True(result.IsSuccess);
        Assert.StartsWith("stripe-", result.Value!.GatewayReference);
    }

    [Fact]
    public async Task VoidAsync_PinnedProvider_IgnoresAmountRouting()
    {
        var (service, _) = CreateService();

        var result = await service.VoidAsync(Guid.NewGuid(), new GatewayReference("GW-1"), "key-1", "stripe");

        Assert.True(result.IsSuccess);
        Assert.StartsWith("stripe-", result.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_UnknownPinnedProvider_FallsBackToRouting()
    {
        var (service, _) = CreateService();

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(100, "USD"), "key-1", "no-such-gateway");

        Assert.True(result.IsSuccess);
        Assert.StartsWith("paystack-", result.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_OpenPinnedCircuit_FallsBackToRouting()
    {
        var (service, registry) = CreateService();
        for (var i = 0; i < 5; i++)
            registry.Get("stripe").CircuitBreaker.RecordFailure();

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(100, "USD"), "key-1", "stripe");

        Assert.True(result.IsSuccess);
        Assert.StartsWith("paystack-", result.Value!.GatewayReference);
    }

    [Fact]
    public async Task CaptureAsync_PinnedDecline_DoesNotFailOver()
    {
        // A decline at the authorizing acquirer is terminal: failing over
        // would charge an acquirer that never authorized.
        var declined = new DecliningProvider();
        var registry = new GatewayProviderRegistry(new IPaymentGatewayProvider[] { declined, new PaystackMockGatewayProvider() });
        var service = new ResilientPaymentGatewayService(
            registry, new GatewayRouter(), NullLogger<ResilientPaymentGatewayService>.Instance);

        var result = await service.CaptureAsync(Guid.NewGuid(), new GatewayReference("GW-1"), new Money(100, "USD"), "key-1", "decliner");

        Assert.False(result.IsSuccess);
    }

    private sealed class DecliningProvider : IPaymentGatewayProvider
    {
        public string Name => "decliner";

        public Task<GatewayResponse> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayResponse(false, null, null, "declined"));

        public Task<GatewayResponse> ConfirmChallengeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, string gatewayReference, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayResponse(false, null, null, "declined"));

        public Task<GatewayResponse> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayResponse(false, null, null, "declined"));

        public Task<GatewayResponse> VoidAsync(Guid merchantId, GatewayReference gatewayRef, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayResponse(false, null, null, "declined"));

        public Task<GatewayResponse> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new GatewayResponse(false, null, null, "declined"));
    }
}
