using BuildingBlocks.Shared.Aggregate;
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
        "PaymentVoidedDomainEvent"
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

        var aggregates = dbContext.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in aggregates)
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
                var outboxMessage = new OutboxMessage(
                    domainEvent.GetType().Name,
                    payload,
                    correlationId);
                dbContext.Set<OutboxMessage>().Add(outboxMessage);

                if (entry.Entity is PaymentIntent intent
                    && WebhookEventTypes.Contains(domainEvent.GetType().Name))
                {
                    dbContext.Set<WebhookEvent>().Add(new WebhookEvent(
                        Guid.NewGuid(),
                        intent.MerchantId,
                        domainEvent.GetType().Name,
                        payload,
                        correlationId));
                }
            }
            entry.Entity.ClearDomainEvents();
        }
    }
}