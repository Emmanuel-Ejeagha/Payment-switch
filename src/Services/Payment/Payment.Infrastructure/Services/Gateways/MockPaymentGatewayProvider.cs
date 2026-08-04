using Payment.Application.DTOs;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services.Gateways;

public abstract class MockPaymentGatewayProvider : IPaymentGatewayProvider
{
    private readonly string _declinedLastFour;
    private readonly string _ref = "GW-";

    protected MockPaymentGatewayProvider(string declinedLastFour)
    {
        _declinedLastFour = declinedLastFour;
    }

    public abstract string Name { get; }

    public Task<GatewayResponse> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CancellationToken cancellationToken = default)
    {
        if (IsDeclined(cardDetails))
            return Task.FromResult(new GatewayResponse(false, null, null, $"Card ending {cardDetails!.LastFour} was declined by {Name}."));

        return Task.FromResult(new GatewayResponse(
            true,
            $"{Name}-AUTH-{Guid.NewGuid().ToString("N")[..8]}",
            $"{Name}-GW-{Guid.NewGuid().ToString("N")[..8]}",
            null));
    }

    public Task<GatewayResponse> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayResponse(true, null, $"{Name}-CAP-{Guid.NewGuid().ToString("N")[..8]}", null));
    }

    public Task<GatewayResponse> VoidAsync(Guid merchantId, GatewayReference gatewayRef, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayResponse(true, null, $"{Name}-VOID-{Guid.NewGuid().ToString("N")[..8]}", null));
    }

    public Task<GatewayResponse> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GatewayResponse(true, null, $"{Name}-REF-{Guid.NewGuid().ToString("N")[..8]}", null));
    }

    private bool IsDeclined(CardDetails? cardDetails)
    {
        if (cardDetails is null) return false;
        return cardDetails.LastFour == _declinedLastFour;
    }
}
