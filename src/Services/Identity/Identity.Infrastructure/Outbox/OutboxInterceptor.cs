using BuildingBlocks.Shared.Aggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Identity.Infrastructure.Outbox;

public class OutboxInterceptor : SaveChangesInterceptor
{
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

    private static void AddOutboxMessages(DbContext? dbContext)
    {
        if (dbContext is null) return;

        var aggregates = dbContext.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in aggregates)
        {
            foreach (var domainEvent in entry.Entity.DomainEvents)
            {
                var outboxMessage = new OutboxMessage(
                    domainEvent.GetType().Name,
                    JsonSerializer.Serialize(domainEvent, domainEvent.GetType()));
                dbContext.Set<OutboxMessage>().Add(outboxMessage);
            }
            entry.Entity.ClearDomainEvents();
        }
    }
}
