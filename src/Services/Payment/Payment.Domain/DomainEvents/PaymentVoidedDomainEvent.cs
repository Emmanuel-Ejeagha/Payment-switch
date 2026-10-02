using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

/// <summary>
/// Emitted when an authorized (or partially-captured) intent is voided.
/// Carries the released amount — the full intent amount from Authorized, or
/// the uncaptured remainder after a partial capture — so downstream ledgers
/// can reverse exactly what is still reserved.
/// </summary>
public record PaymentVoidedDomainEvent(Guid IntentId, Guid MerchantId, Money Amount) : DomainEvent;