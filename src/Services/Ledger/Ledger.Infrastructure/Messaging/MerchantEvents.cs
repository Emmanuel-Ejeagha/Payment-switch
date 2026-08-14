namespace Ledger.Infrastructure.Messaging;

/// <summary>Merchant lifecycle payloads consumed from <c>merchant.events</c>.</summary>
public record MerchantOnboardedPayload(Guid MerchantId, string BusinessName, string Email);