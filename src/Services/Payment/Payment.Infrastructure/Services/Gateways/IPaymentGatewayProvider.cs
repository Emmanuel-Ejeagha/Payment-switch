using Payment.Application.DTOs;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services.Gateways;

public interface IPaymentGatewayProvider
{
    string Name { get; }

    /// <param name="securityCode">
    /// Transient CVC forwarded to the acquirer for this authorization only.
    /// Providers must not persist or log it.
    /// </param>
    Task<GatewayResponse> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CardSecurityCode? securityCode = null, CancellationToken cancellationToken = default);
    Task<GatewayResponse> ConfirmChallengeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, string gatewayReference, CardSecurityCode? securityCode = null, CancellationToken cancellationToken = default);
    Task<GatewayResponse> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
    Task<GatewayResponse> VoidAsync(Guid merchantId, GatewayReference gatewayRef, CancellationToken cancellationToken = default);
    Task<GatewayResponse> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
}
