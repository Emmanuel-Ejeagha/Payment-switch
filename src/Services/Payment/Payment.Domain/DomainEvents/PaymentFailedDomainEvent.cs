using BuildingBlocks.Shared.Events;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.DomainEvents;

/// <summary>
/// Emitted when an intent terminates failed before any money moved
/// (Pending / RequiresAction / Processing only — never post-authorization).
/// </summary>
public record PaymentFailedDomainEvent(Guid IntentId, Guid MerchantId, Money Amount) : DomainEvent;
