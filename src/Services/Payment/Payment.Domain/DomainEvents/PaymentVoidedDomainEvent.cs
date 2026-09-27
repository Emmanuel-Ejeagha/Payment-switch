using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

/// <summary>
/// Emitted when an authorized intent is voided. Carries the released amount
/// (void is only legal from Authorized, so this is the full intent amount)
/// so downstream ledgers can reverse the reservation.
/// </summary>
public record PaymentVoidedDomainEvent(Guid IntentId, Guid MerchantId, Money Amount) : DomainEvent;