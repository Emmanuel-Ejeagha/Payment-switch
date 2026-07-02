using BuildingBlocks.Shared.Events;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.DomainEvents;

public record FundsRefundedEvent(Guid MerchantId, Money Amount, string CorrelationId) : DomainEvent;