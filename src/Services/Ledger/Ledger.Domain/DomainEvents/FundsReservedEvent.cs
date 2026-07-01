using BuildingBlocks.Shared.Events;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.DomainEvents;

public record FundsReservedEvent(Guid MerchantId, Money Amount, string CorrelationId) : DomainEvent;