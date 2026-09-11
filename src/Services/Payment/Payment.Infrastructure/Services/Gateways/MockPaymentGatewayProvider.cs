using Payment.Application.DTOs;
using Payment.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace Payment.Infrastructure.Services.Gateways;

public abstract class MockPaymentGatewayProvider : IPaymentGatewayProvider
{
    private readonly string _declinedLastFour;
    private readonly string _challengeLastFour;

    // Acquirer-side idempotency simulation: a repeated non-empty key returns the
    // original response instead of moving money again. Real providers implement
    // this durably; the mock keeps it process-scoped for tests and local runs.
    private readonly ConcurrentDictionary<string, GatewayResponse> _idempotentResponses = new();

    protected MockPaymentGatewayProvider(string declinedLastFour, string challengeLastFour)
    {
        _declinedLastFour = declinedLastFour;
        _challengeLastFour = challengeLastFour;
    }

    public abstract string Name { get; }

    private Task<GatewayResponse> ExecuteOnce(string? idempotencyKey, Func<GatewayResponse> produce)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotentResponses.TryGetValue(idempotencyKey, out var cached))
            return Task.FromResult(cached);

        var response = produce();

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            _idempotentResponses.TryAdd(idempotencyKey, response);

        return Task.FromResult(response);
    }

    public Task<GatewayResponse> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        return ExecuteOnce(idempotencyKey, () =>
        {
            if (IsDeclined(cardDetails))
                return new GatewayResponse(false, null, null, $"Card ending {cardDetails!.LastFour} was declined by {Name}.");

            if (RequiresChallenge(cardDetails))
                return new GatewayResponse(
                    true,
                    null,
                    $"{Name}-GW-{Guid.NewGuid().ToString("N")[..8]}",
                    null,
                    RequiresChallenge: true);

            return new GatewayResponse(
                true,
                $"{Name}-AUTH-{Guid.NewGuid().ToString("N")[..8]}",
                $"{Name}-GW-{Guid.NewGuid().ToString("N")[..8]}",
                null);
        });
    }

    public Task<GatewayResponse> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        return ExecuteOnce(idempotencyKey, () =>
            new GatewayResponse(true, null, $"{Name}-CAP-{Guid.NewGuid().ToString("N")[..8]}", null));
    }

    public Task<GatewayResponse> VoidAsync(Guid merchantId, GatewayReference gatewayRef, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        return ExecuteOnce(idempotencyKey, () =>
            new GatewayResponse(true, null, $"{Name}-VOID-{Guid.NewGuid().ToString("N")[..8]}", null));
    }

    public Task<GatewayResponse> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        return ExecuteOnce(idempotencyKey, () =>
            new GatewayResponse(true, null, $"{Name}-REF-{Guid.NewGuid().ToString("N")[..8]}", null));
    }

    public Task<GatewayResponse> ConfirmChallengeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, string gatewayReference, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        return ExecuteOnce(idempotencyKey, () =>
        {
            if (IsDeclined(cardDetails))
                return new GatewayResponse(false, null, null, $"Card ending {cardDetails!.LastFour} was declined by {Name}.");

            return new GatewayResponse(
                true,
                $"{Name}-AUTH-{Guid.NewGuid().ToString("N")[..8]}",
                gatewayReference,
                null);
        });
    }

    private bool IsDeclined(CardDetails? cardDetails)
    {
        if (cardDetails is null) return false;
        return cardDetails.LastFour == _declinedLastFour;
    }

    private bool RequiresChallenge(CardDetails? cardDetails)
    {
        if (cardDetails is null) return false;
        return cardDetails.LastFour == _challengeLastFour;
    }
}
