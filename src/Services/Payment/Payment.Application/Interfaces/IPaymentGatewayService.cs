using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Interfaces;

public interface IPaymentGatewayService
{
    /// <param name="securityCode">
    /// Transient CVC for this authorization only. Never persisted — see
    /// <see cref="CardSecurityCode"/>. Null when the cardholder is not present
    /// (recurring subscription cycles, merchant-initiated retries).
    /// </param>
    Task<Result<GatewayResponse>> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CardSecurityCode? securityCode = null, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> ConfirmChallengeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, string gatewayReference, CardSecurityCode? securityCode = null, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> VoidAsync(Guid merchantId, GatewayReference gatewayRef, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, CancellationToken cancellationToken = default);
}
