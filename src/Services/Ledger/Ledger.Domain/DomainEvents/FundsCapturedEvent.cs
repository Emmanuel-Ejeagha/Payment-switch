using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Events;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.DomainEvents;

public record FundsCapturedEvent(Guid MerchantId, Money Amount, string CorrelationId) : DomainEvent;