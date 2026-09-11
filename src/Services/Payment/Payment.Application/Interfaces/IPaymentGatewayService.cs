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
    /// <param name="idempotencyKey">
    /// Crash-safety key for this gateway operation (normally the command's
    /// idempotency key). Providers MUST return the original response when the
    /// same non-empty key is repeated instead of moving money again, so a
    /// retry after a crash between gateway success and local commit cannot
    /// double-charge. Keys must be unique per gateway operation.
    /// </param>
    /// <param name="providerName">
    /// Pins this follow-on operation (capture/void/refund/confirm) to the
    /// acquirer that authorized the intent. Null (authorize-time) keeps normal
    /// routing with failover; a pinned call fails over only when the pinned
    /// provider is unknown or its circuit is open.
    /// </param>
    Task<Result<GatewayResponse>> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CardSecurityCode? securityCode = null, string? idempotencyKey = null, string? providerName = null, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> ConfirmChallengeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, string gatewayReference, CardSecurityCode? securityCode = null, string? idempotencyKey = null, string? providerName = null, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, string? providerName = null, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> VoidAsync(Guid merchantId, GatewayReference gatewayRef, string? idempotencyKey = null, string? providerName = null, CancellationToken cancellationToken = default);
    Task<Result<GatewayResponse>> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, string? providerName = null, CancellationToken cancellationToken = default);
}
