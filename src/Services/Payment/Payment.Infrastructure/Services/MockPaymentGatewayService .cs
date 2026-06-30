using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services;

public class MockPaymentGatewayService : IPaymentGatewayService
{
    public Task<Result<GatewayResponse>> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CancellationToken cancellationToken = default)
    {
        var response = new GatewayResponse(
            true,
            $"AUTH-{Guid.NewGuid().ToString("N")[..8]}",
            $"GW-{Guid.NewGuid().ToString("N")[..8]}",
            null);
        return Task.FromResult(Result<GatewayResponse>.Success(response));
    }

    public Task<Result<GatewayResponse>> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default)
    {
        var response = new GatewayResponse(true, null, $"CAP-{Guid.NewGuid().ToString("N")[..8]}", null);
        return Task.FromResult(Result<GatewayResponse>.Success(response));
    }

    public Task<Result<GatewayResponse>> VoidAsync(Guid merchantId, GatewayReference gatewayRef, CancellationToken cancellationToken = default)
    {
        var response = new GatewayResponse(true, null, null, null);
        return Task.FromResult(Result<GatewayResponse>.Success(response));
    }

    public Task<Result<GatewayResponse>> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default)
    {
        var response = new GatewayResponse(true, null, $"REF-{Guid.NewGuid().ToString("N")[..8]}", null);
        return Task.FromResult(Result<GatewayResponse>.Success(response));
    }
}