namespace BuildingBlocks.Shared.Messaging;

/// <summary>
/// Single source of truth for payment event routing keys. The set of events
/// Payment publishes must be a subset of the union of Ledger + Notification
/// consumer bindings, or messages drop into a void. Centralizing the channel
/// names here prevents drift and lets a unit test assert inbox coverage.
/// </summary>
public static class PaymentEventChannels
{
    /// <summary>
    /// Event types Payment writes to the outbox with a real RabbitMQ consumer.
    /// Must stay in sync with the consumer binding lists below.
    /// </summary>
    public static readonly IReadOnlySet<string> PublishedPaymentEvents = new HashSet<string>
    {
        "PaymentIntentCreatedDomainEvent",
        "PaymentAuthorizedDomainEvent",
        "PaymentCapturedDomainEvent",
        "PaymentRefundedDomainEvent",
        "PaymentVoidedDomainEvent",
        "PaymentFailedDomainEvent",
        "PaymentExpiredDomainEvent"
    };

    /// <summary>Payment event types Ledger consumes (financial postings).</summary>
    public static readonly IReadOnlySet<string> LedgerBoundPaymentEvents = new HashSet<string>
    {
        "PaymentAuthorizedDomainEvent",
        "PaymentCapturedDomainEvent",
        "PaymentRefundedDomainEvent",
        "PaymentVoidedDomainEvent"
    };

    /// <summary>Payment event types Notification consumes (user-facing).</summary>
    public static readonly IReadOnlySet<string> NotificationBoundPaymentEvents = new HashSet<string>
    {
        "PaymentIntentCreatedDomainEvent",
        "PaymentAuthorizedDomainEvent",
        "PaymentCapturedDomainEvent",
        "PaymentRefundedDomainEvent",
        "PaymentVoidedDomainEvent",
        "PaymentFailedDomainEvent",
        "PaymentExpiredDomainEvent"
    };

    /// <summary>
    /// Every event Payment publishes must be bound by at least one consumer.
    /// Use in unit tests as a coverage invariant.
    /// </summary>
    public static bool HasInboxCoverage(string eventType) =>
        LedgerBoundPaymentEvents.Contains(eventType) ||
        NotificationBoundPaymentEvents.Contains(eventType);
}
