using Payment.Application.DTOs;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services.Gateways;

public interface IPaymentGatewayProvider
{
    string Name { get; }

    Task<GatewayResponse> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CancellationToken cancellationToken = default);
    Task<GatewayResponse> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
    Task<GatewayResponse> VoidAsync(Guid merchantId, GatewayReference gatewayRef, CancellationToken cancellationToken = default);
    Task<GatewayResponse> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
}
