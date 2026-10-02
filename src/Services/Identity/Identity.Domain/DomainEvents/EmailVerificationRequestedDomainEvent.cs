using BuildingBlocks.Shared.Events;

namespace Identity.Domain.DomainEvents;

/// <summary>
/// Raised when a verification token is issued (registration or resend).
/// Carries the RAW token so the Notification service can build the verification
/// link: the raw value must never be logged, only persisted via the outbox row
/// and consumed once. Single-use + TTL + rotation bound its lifetime.
/// </summary>
public record EmailVerificationRequestedDomainEvent(
    Guid UserId,
    string Email,
    string Token,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc) : DomainEvent;
