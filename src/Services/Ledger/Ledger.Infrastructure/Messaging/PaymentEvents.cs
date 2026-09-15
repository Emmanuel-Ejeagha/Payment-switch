namespace Ledger.Infrastructure.Messaging;

public record PaymentAuthorizedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, string AuthorizationCode, string GatewayReference, DateTime OccurredOn = default);
public record PaymentCapturedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId, DateTime OccurredOn = default);
public record PaymentRefundedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId, DateTime OccurredOn = default);
public record PaymentVoidedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, DateTime OccurredOn = default);
public record MoneyPayload(long Amount, string Currency);