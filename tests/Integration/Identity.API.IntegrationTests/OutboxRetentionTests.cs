using Identity.Infrastructure.Outbox;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Identity.API.IntegrationTests;

/// <summary>
/// Proves the processed-outbox sweep bounds raw-token retention: processed
/// rows older than the messaging window are hard-deleted (transport envelopes,
/// not business history), while recent and unprocessed rows are untouched.
/// </summary>
public class OutboxRetentionTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public OutboxRetentionTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RunOnceAsync_DeletesOnlyOldProcessedRows()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var oldRow = new OutboxMessage("EmailVerificationRequestedDomainEvent", "{}");
        var recentRow = new OutboxMessage("EmailVerificationRequestedDomainEvent", "{}");
        var pendingRow = new OutboxMessage("EmailVerificationRequestedDomainEvent", "{}");
        db.OutboxMessages.AddRange(oldRow, recentRow, pendingRow);
        await db.SaveChangesAsync();
        oldRow.MarkAsProcessed();
        recentRow.MarkAsProcessed();
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"OutboxMessages\" SET \"OccurredOn\" = @p0 WHERE \"Id\" = @p1",
            DateTime.UtcNow.AddDays(-10), oldRow.Id);

        var service = new OutboxRetentionService(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxRetentionOptions { MessageRetentionDays = 7, BatchSize = 100 }),
            NullLogger<OutboxRetentionService>.Instance);

        var deleted = await service.RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.False(await db.OutboxMessages.AnyAsync(m => m.Id == oldRow.Id));
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Id == recentRow.Id));
        Assert.True(await db.OutboxMessages.AnyAsync(m => m.Id == pendingRow.Id));
    }
}
