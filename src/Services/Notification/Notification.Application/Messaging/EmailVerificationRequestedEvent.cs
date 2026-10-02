namespace Notification.Application.Messaging;

/// <summary>
/// Verification request published by Identity (exchange <c>identity.events</c>).
/// Mirrors the Identity-side contract; the raw token travels here so the
/// verification link can be built (never logged).
/// </summary>
public record EmailVerificationRequestedEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc);
