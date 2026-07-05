using BuildingBlocks.Shared.Events;

namespace Merchant.Domain.DomainEvents;

public record MerchantActivatedEvent(Guid MerchantId) : DomainEvent;