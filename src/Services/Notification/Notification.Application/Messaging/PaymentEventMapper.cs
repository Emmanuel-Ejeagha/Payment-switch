using Notification.Application.Features.Commands.CreateNotification;

namespace Notification.Application.Messaging;

public record PaymentAuthorizedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, string AuthorizationCode, string GatewayReference);
public record PaymentCapturedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId);
public record PaymentRefundedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId);
public record PaymentIntentCreatedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, string IdempotencyKey);
public record PaymentVoidedEvent(Guid IntentId, Guid MerchantId);
public record MoneyPayload(long Amount, string Currency);

/// <summary>
/// Maps payment events to notification commands. The recipient is resolved at
/// consume time (the merchant's contact email) and is never a placeholder.
/// </summary>
public static class PaymentEventMapper
{
    public static CreateNotificationCommand MapAuthorized(PaymentAuthorizedEvent e, string recipient, string body) =>
        new(recipient, "email", "Payment Authorized",
            $"A payment of {e.Amount.Amount} {e.Amount.Currency} was authorized for your account.", null, body);

    public static CreateNotificationCommand MapCaptured(PaymentCapturedEvent e, string recipient, string body) =>
        new(recipient, "email", "Payment Captured",
            $"A payment of {e.Amount.Amount} {e.Amount.Currency} was captured for your account.", null, body);

    public static CreateNotificationCommand MapRefunded(PaymentRefundedEvent e, string recipient, string body) =>
        new(recipient, "email", "Payment Refunded",
            $"A refund of {e.Amount.Amount} {e.Amount.Currency} was processed for your account.", null, body);

    public static CreateNotificationCommand MapIntentCreated(PaymentIntentCreatedEvent e, string recipient, string body) =>
        new(recipient, "email", "Payment Started",
            $"A payment of {e.Amount.Amount} {e.Amount.Currency} was initiated for your account.", null, body);

    public static CreateNotificationCommand MapVoided(PaymentVoidedEvent e, string recipient, string body) =>
        new(recipient, "email", "Payment Voided",
            $"A payment was voided for your account.", null, body);
}
