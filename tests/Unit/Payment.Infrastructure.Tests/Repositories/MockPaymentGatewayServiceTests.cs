using Microsoft.Extensions.Logging.Abstractions;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Services.Gateways;

namespace Payment.Infrastructure.Tests.Services;

public class MockPaymentGatewayServiceTests
{
    private static ResilientPaymentGatewayService CreateService(out GatewayProviderRegistry registry, out GatewayRouter router)
    {
        var providers = new IPaymentGatewayProvider[]
        {
            new PaystackMockGatewayProvider(),
            new StripeMockGatewayProvider()
        };
        registry = new GatewayProviderRegistry(providers);
        router = new GatewayRouter();
        return new ResilientPaymentGatewayService(registry, router, NullLogger<ResilientPaymentGatewayService>.Instance);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldReturnSuccessWithCodes()
    {
        var service = CreateService(out _, out _);
        var result = await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), null);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("paystack-", result.Value!.AuthorizationCode);
        Assert.StartsWith("paystack-", result.Value.GatewayReference);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldRouteByCardBrand()
    {
        var service = CreateService(out _, out _);
        var result = await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), new CardDetails("1234", "Mastercard"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("stripe-", result.Value!.AuthorizationCode);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldRouteHighValueToStripe()
    {
        var service = CreateService(out _, out _);
        var result = await service.AuthorizeAsync(Guid.NewGuid(), new Money(1000_00, "NGN"), new CardDetails("1234", "Visa"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("stripe-", result.Value!.AuthorizationCode);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldFallBackWhenPrimaryDeclines()
    {
        var service = CreateService(out _, out _);

        // stripe (primary for Mastercard) declines 7777 -> fall back to paystack
        var result = await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), new CardDetails("7777", "Mastercard"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("paystack-", result.Value!.AuthorizationCode);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldFailWhenAllProvidersDecline()
    {
        var providers = new IPaymentGatewayProvider[] { new PaystackMockGatewayProvider() };
        var registry = new GatewayProviderRegistry(providers);
        var router = new GatewayRouter();
        var service = new ResilientPaymentGatewayService(registry, router, NullLogger<ResilientPaymentGatewayService>.Instance);

        // paystack is the only fallback (stripe not registered) and declines 9999
        var result = await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), new CardDetails("9999", "Amex"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Payment.GatewayUnavailable", result.Errors.First().Code);
    }

    [Fact]
    public async Task AuthorizeAsync_ShouldOpenCircuitAfterRepeatedFailures()
    {
        var providers = new IPaymentGatewayProvider[] { new PaystackMockGatewayProvider() };
        var registry = new GatewayProviderRegistry(providers);
        var router = new GatewayRouter();
        var service = new ResilientPaymentGatewayService(registry, router, NullLogger<ResilientPaymentGatewayService>.Instance);

        // 5 failures trip the breaker (default threshold); 9999 is declined by paystack
        for (var i = 0; i < 5; i++)
        {
            await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), new CardDetails("9999", "Amex"));
        }

        Assert.False(registry.Get("paystack").CircuitBreaker.CanProceed());
    }
}
