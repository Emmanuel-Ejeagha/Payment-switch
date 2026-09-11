using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services.Gateways;

public class ResilientPaymentGatewayService : IPaymentGatewayService
{
    private readonly GatewayProviderRegistry _registry;
    private readonly GatewayRouter _router;
    private readonly ILogger<ResilientPaymentGatewayService> _logger;

    public ResilientPaymentGatewayService(
        GatewayProviderRegistry registry,
        GatewayRouter router,
        ILogger<ResilientPaymentGatewayService> logger)
    {
        _registry = registry;
        _router = router;
        _logger = logger;
    }

    public async Task<Result<GatewayResponse>> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var ordered = _router.Resolve(amount, cardDetails, _registry.All);

        foreach (var entry in ordered)
        {
            if (!entry.CircuitBreaker.CanProceed())
            {
                _logger.LogWarning("Gateway {Gateway} is open; skipping", entry.Provider.Name);
                continue;
            }

            try
            {
                var response = await entry.Provider.AuthorizeAsync(merchantId, amount, cardDetails, securityCode, idempotencyKey, cancellationToken);
                if (response.Success)
                {
                    entry.CircuitBreaker.RecordSuccess();
                    return Result<GatewayResponse>.Success(response);
                }

                entry.CircuitBreaker.RecordFailure();
                _logger.LogWarning("Gateway {Gateway} declined authorize: {Error}", entry.Provider.Name, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                entry.CircuitBreaker.RecordFailure();
                _logger.LogError(ex, "Gateway {Gateway} threw during authorize", entry.Provider.Name);
            }
        }

        return new Error("Payment.GatewayUnavailable", "All payment gateways are unavailable.");
    }

    public async Task<Result<GatewayResponse>> ConfirmChallengeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, string gatewayReference, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var ordered = _router.Resolve(amount, cardDetails, _registry.All);

        foreach (var entry in ordered)
        {
            if (!entry.CircuitBreaker.CanProceed())
            {
                _logger.LogWarning("Gateway {Gateway} is open; skipping", entry.Provider.Name);
                continue;
            }

            try
            {
                var response = await entry.Provider.ConfirmChallengeAsync(merchantId, amount, cardDetails, gatewayReference, securityCode, idempotencyKey, cancellationToken);
                if (response.Success)
                {
                    entry.CircuitBreaker.RecordSuccess();
                    return Result<GatewayResponse>.Success(response);
                }

                entry.CircuitBreaker.RecordFailure();
                _logger.LogWarning("Gateway {Gateway} declined challenge confirm: {Error}", entry.Provider.Name, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                entry.CircuitBreaker.RecordFailure();
                _logger.LogError(ex, "Gateway {Gateway} threw during challenge confirm", entry.Provider.Name);
            }
        }

        return new Error("Payment.GatewayUnavailable", "All payment gateways are unavailable.");
    }

    public async Task<Result<GatewayResponse>> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var ordered = _router.Resolve(amount, null, _registry.All);

        foreach (var entry in ordered)
        {
            if (!entry.CircuitBreaker.CanProceed())
            {
                _logger.LogWarning("Gateway {Gateway} is open; skipping", entry.Provider.Name);
                continue;
            }

            try
            {
                var response = await entry.Provider.CaptureAsync(merchantId, gatewayRef, amount, idempotencyKey, cancellationToken);
                if (response.Success)
                {
                    entry.CircuitBreaker.RecordSuccess();
                    return Result<GatewayResponse>.Success(response);
                }

                entry.CircuitBreaker.RecordFailure();
                _logger.LogWarning("Gateway {Gateway} declined capture: {Error}", entry.Provider.Name, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                entry.CircuitBreaker.RecordFailure();
                _logger.LogError(ex, "Gateway {Gateway} threw during capture", entry.Provider.Name);
            }
        }

        return new Error("Payment.GatewayUnavailable", "All payment gateways are unavailable.");
    }

    public async Task<Result<GatewayResponse>> VoidAsync(Guid merchantId, GatewayReference gatewayRef, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var ordered = _router.Resolve(new Money(0, "NGN"), null, _registry.All);

        foreach (var entry in ordered)
        {
            if (!entry.CircuitBreaker.CanProceed())
            {
                _logger.LogWarning("Gateway {Gateway} is open; skipping", entry.Provider.Name);
                continue;
            }

            try
            {
                var response = await entry.Provider.VoidAsync(merchantId, gatewayRef, idempotencyKey, cancellationToken);
                if (response.Success)
                {
                    entry.CircuitBreaker.RecordSuccess();
                    return Result<GatewayResponse>.Success(response);
                }

                entry.CircuitBreaker.RecordFailure();
                _logger.LogWarning("Gateway {Gateway} declined void: {Error}", entry.Provider.Name, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                entry.CircuitBreaker.RecordFailure();
                _logger.LogError(ex, "Gateway {Gateway} threw during void", entry.Provider.Name);
            }
        }

        return new Error("Payment.GatewayUnavailable", "All payment gateways are unavailable.");
    }

    public async Task<Result<GatewayResponse>> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var ordered = _router.Resolve(amount, null, _registry.All);

        foreach (var entry in ordered)
        {
            if (!entry.CircuitBreaker.CanProceed())
            {
                _logger.LogWarning("Gateway {Gateway} is open; skipping", entry.Provider.Name);
                continue;
            }

            try
            {
                var response = await entry.Provider.RefundAsync(merchantId, gatewayRef, amount, idempotencyKey, cancellationToken);
                if (response.Success)
                {
                    entry.CircuitBreaker.RecordSuccess();
                    return Result<GatewayResponse>.Success(response);
                }

                entry.CircuitBreaker.RecordFailure();
                _logger.LogWarning("Gateway {Gateway} declined refund: {Error}", entry.Provider.Name, response.ErrorMessage);
            }
            catch (Exception ex)
            {
                entry.CircuitBreaker.RecordFailure();
                _logger.LogError(ex, "Gateway {Gateway} threw during refund", entry.Provider.Name);
            }
        }

        return new Error("Payment.GatewayUnavailable", "All payment gateways are unavailable.");
    }
}
