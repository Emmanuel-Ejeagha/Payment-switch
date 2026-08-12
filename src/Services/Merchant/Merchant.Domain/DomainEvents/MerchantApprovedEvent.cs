using BuildingBlocks.Shared.Events;

namespace Merchant.Domain.DomainEvents;

public record MerchantApprovedEvent(Guid MerchantId) : DomainEvent;