using BuildingBlocks.Shared.Messaging;
using BuildingBlocks.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Merchant.Infrastructure.Outbox;

public class OutboxInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Event types with a real RabbitMQ consumer. Everything else carries no
    /// cross-service value and is not written to the outbox — publishing it
    /// would drop it into a void. See docs/messaging-registry.md.
    /// </summary>
    private static readonly HashSet<string> PublishedEventTypes = new()
    {
        "MerchantOnboardedEvent"
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
                if (!PublishedEventTypes.Contains(domainEvent.GetType().Name))
                    continue;

                var outboxMessage = new OutboxMessage(
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    correlationId,
                    traceParent);
                dbContext.Set<OutboxMessage>().Add(outboxMessage);
            }
            entry.Entity.ClearDomainEvents();
        }
    }
}