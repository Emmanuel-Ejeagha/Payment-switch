using BuildingBlocks.Shared.Events;

namespace Merchant.Domain.DomainEvents;

public record MerchantConfigurationUpdatedEvent(Guid MerchantId) : DomainEvent;