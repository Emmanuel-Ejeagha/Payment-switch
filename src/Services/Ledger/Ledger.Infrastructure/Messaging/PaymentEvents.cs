namespace Ledger.Infrastructure.Messaging;

public record PaymentAuthorizedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, string AuthorizationCode, string GatewayReference);
public record PaymentCapturedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId);
public record PaymentRefundedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId);
public record MoneyPayload(decimal Amount, string Currency);