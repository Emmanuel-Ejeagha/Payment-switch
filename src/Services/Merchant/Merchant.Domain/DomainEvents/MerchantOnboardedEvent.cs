using BuildingBlocks.Shared.Events;

namespace Merchant.Domain.DomainEvents;

public record MerchantOnboardedEvent(Guid MerchantId, string BusinessName, string Email) : DomainEvent;