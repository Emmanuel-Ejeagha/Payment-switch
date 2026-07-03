using BuildingBlocks.Shared.Events;

namespace Settlement.Domain.DomainEvents;

public record SettlementBatchCompletedEvent(Guid BatchId, DateTime BatchDate, decimal TotalAmount, string Currency) : DomainEvent;