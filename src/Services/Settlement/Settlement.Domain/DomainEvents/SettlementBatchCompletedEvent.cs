using BuildingBlocks.Shared.Events;

namespace Settlement.Domain.DomainEvents;

public record SettlementBatchCompletedEvent(Guid BatchId, DateTime BatchDate, long TotalAmount, string Currency) : DomainEvent;