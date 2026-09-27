namespace Notification.Domain;

/// <summary>
/// The notification event types a recipient can opt in or out of. Values match
/// the RabbitMQ routing keys the Notification consumer binds to.
/// </summary>
public static class NotificationEventTypes
{
    public const string PaymentAuthorized = "PaymentAuthorizedDomainEvent";
    public const string PaymentCaptured = "PaymentCapturedDomainEvent";
    public const string PaymentRefunded = "PaymentRefundedDomainEvent";
    public const string PaymentIntentCreated = "PaymentIntentCreatedDomainEvent";
    public const string PaymentVoided = "PaymentVoidedDomainEvent";

    public static readonly IReadOnlyList<string> All = new[]
    {
        PaymentAuthorized,
        PaymentCaptured,
        PaymentRefunded,
        PaymentIntentCreated,
        PaymentVoided
    };

    public static bool IsValid(string eventType) =>
        eventType is PaymentAuthorized or PaymentCaptured or PaymentRefunded or PaymentIntentCreated or PaymentVoided;
}
