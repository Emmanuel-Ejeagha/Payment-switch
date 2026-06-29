using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Interfaces;

public interface IPaymentGatewayService
{
    Task<Result<GatewayResponse>> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> VoidAsync(Guid merchantId, GatewayReference gatewayRef, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
}