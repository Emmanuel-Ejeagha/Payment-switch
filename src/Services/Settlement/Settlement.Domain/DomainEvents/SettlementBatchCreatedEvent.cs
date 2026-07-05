using BuildingBlocks.Shared.Events;

namespace Settlement.Domain.DomainEvents;

public record SettlementBatchCreatedEvent(Guid BatchId, DateTime BatchDate, int PayoutCount) : DomainEvent;