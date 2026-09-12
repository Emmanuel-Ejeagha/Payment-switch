using BuildingBlocks.Shared.Aggregate;
using BuildingBlocks.Shared.Messaging;
using BuildingBlocks.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Payment.Domain.Entities;
using System.Text.Json;

namespace Payment.Infrastructure.Outbox;

public class OutboxInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> WebhookEventTypes = new()
    {
        "PaymentIntentCreatedDomainEvent",
        "PaymentAuthorizedDomainEvent",
        "PaymentRequiresActionDomainEvent",
        "PaymentProcessingDomainEvent",
        "PaymentCapturedDomainEvent",
        "PaymentRefundedDomainEvent",
        "PaymentVoidedDomainEvent",
        "PaymentFailedDomainEvent",
        "PaymentExpiredDomainEvent"
    };

    /// <summary>
    /// Event types with a real RabbitMQ consumer. Everything else is only ever
    /// delivered in-process (webhooks) or carries no cross-service value, so it
    /// is not written to the outbox — publishing it would drop it into a void.
    /// See docs/messaging-registry.md.
    /// </summary>
    private static readonly HashSet<string> PublishedEventTypes = new()
    {
        "PaymentIntentCreatedDomainEvent",
        "PaymentAuthorizedDomainEvent",
        "PaymentCapturedDomainEvent",
        "PaymentRefundedDomainEvent",
        "PaymentVoidedDomainEvent",
        "PaymentFailedDomainEvent",
        "PaymentExpiredDomainEvent"
    };

    private readonly ICorrelationIdProvider _correlationIdProvider;

    public OutboxInterceptor(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddOutboxMessages(DbContext? dbContext)
    {
        if (dbContext is null) return;

        var correlationId = string.IsNullOrWhiteSpace(_correlationIdProvider.CorrelationId)
            ? null
            : _correlationIdProvider.CorrelationId;
        var traceParent = RabbitMqTracing.CurrentTraceParent();

        var aggregates = dbContext.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in aggregates)
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                var eventType = domainEvent.GetType().Name;

                if (PublishedEventTypes.Contains(eventType))
                {
                    var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
                    // Downstream journal postings are keyed by correlation: sibling
                    // events flushed in one SaveChanges (e.g. authorize+capture on
                    // auto-capture) must not share a correlation, or the second
                    // posting violates the unique journal index and DLQs with
                    // funds stuck reserved. Scope each row to its event; the
                    // stored value keeps redeliveries stable.
                    var rowCorrelationId = correlationId is null || entry.Entity is not PaymentIntent scopedIntent
                        ? correlationId
                        : $"{correlationId}:{scopedIntent.Id}:{eventType}";
                    var outboxMessage = new OutboxMessage(
                        eventType,
                        payload,
                        rowCorrelationId,
                        traceParent);
                    dbContext.Set<OutboxMessage>().Add(outboxMessage);
                }

                if (entry.Entity is PaymentIntent intent
                    && WebhookEventTypes.Contains(eventType))
                {
                    dbContext.Set<WebhookEvent>().Add(new WebhookEvent(
                        Guid.NewGuid(),
                        intent.MerchantId,
                        eventType,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        correlationId));
                }
            }
            entry.Entity.ClearDomainEvents();
        }
    }
}