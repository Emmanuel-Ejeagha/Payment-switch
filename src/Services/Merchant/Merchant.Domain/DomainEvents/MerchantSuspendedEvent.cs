using BuildingBlocks.Shared.Events;

namespace Merchant.Domain.DomainEvents;

public record MerchantSuspendedEvent(Guid MerchantId) : DomainEvent;