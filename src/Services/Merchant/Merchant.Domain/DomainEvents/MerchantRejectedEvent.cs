using BuildingBlocks.Shared.Events;

namespace Merchant.Domain.DomainEvents;

public record MerchantRejectedEvent(Guid MerchantId, string Reason) : DomainEvent;