using BuildingBlocks.Shared.Retention;
using Ledger.Domain.Entities;
using Ledger.Domain.ValueObjects;
using Ledger.Infrastructure.Inbox;
using Ledger.Infrastructure.Outbox;
using Ledger.Infrastructure.Persistence;
using Ledger.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// TASK-042: the Ledger retention sweep archives expired journal entries (7y
/// financial retention) and processed inbox/outbox messages (7d) into
/// <c>ArchivedRecords</c>; the account row itself is never archived.
/// </summary>
public class RetentionTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public RetentionTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JournalEntry_Expired_IsArchivedAndRemoved_FreshStays_AccountSurvives()
    {
        var accountId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var account = new LedgerAccount(accountId, merchantId, "USD");
            account.ReserveFunds(new Money(5000, "USD"), new CorrelationId("ret-reserve-old"));
            account.CaptureFunds(new Money(5000, "USD"), new CorrelationId("ret-capture-old"));
            account.ReserveFunds(new Money(200, "USD"), new CorrelationId("ret-reserve-fresh"));
            db.LedgerAccounts.Add(account);
            await db.SaveChangesAsync();

            // Backdate only the first two journal entries past the 7y cutoff.
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"JournalEntries\" SET \"Timestamp\" = {0} WHERE \"LedgerAccountId\" = {1} AND \"CorrelationId\" IN ({2}, {3})",
                DateTime.UtcNow.AddYears(-8), accountId, "ret-reserve-old", "ret-capture-old");
        }

        await RunRetentionAsync();

        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var accountAfter = await vdb.LedgerAccounts
            .Include(a => a.Journal)
            .FirstAsync(a => a.Id == accountId);

        Assert.NotNull(accountAfter);
        Assert.Single(accountAfter.Journal);
        Assert.Contains(accountAfter.Journal, j => j.Description == "Funds reserved" && j.Amount.Amount == 200);

        var archivedCount = await vdb.ArchivedRecords.CountAsync(r => r.SourceTable == "JournalEntries");
        Assert.True(archivedCount >= 2, $"expected >= 2 archived journal entries, got {archivedCount}");
        var archivedJournal = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "JournalEntries")
            .ToListAsync();
        var archive = archivedJournal.SingleOrDefault(r => r.Payload.Contains("ret-reserve-old"));
        Assert.NotNull(archive);
        using (var doc = System.Text.Json.JsonDocument.Parse(archive!.Payload))
        {
            Assert.Equal(5000, doc.RootElement.GetProperty("Amount").GetProperty("Amount").GetInt64());
        }
    }

    [Fact]
    public async Task InboxAndOutbox_ProcessedAndExpired_AreArchivedAndRemoved()
    {
        InboxMessage inbox;
        OutboxMessage outbox;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            inbox = new InboxMessage(Guid.NewGuid().ToString(), "PaymentAuthorizedDomainEvent", """{"amount":5000}""");
            inbox.MarkAsProcessed();
            outbox = new OutboxMessage("FundsReservedEvent", """{"amount":5000}""");
            outbox.MarkAsProcessed();
            db.InboxMessages.Add(inbox);
            db.OutboxMessages.Add(outbox);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"InboxMessages\" SET \"ProcessedAt\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddDays(-30), inbox.Id);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"OutboxMessages\" SET \"OccurredOn\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddDays(-30), outbox.Id);
        }

        await RunRetentionAsync();

        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await vdb.InboxMessages.FirstOrDefaultAsync(m => m.Id == inbox.Id));
        Assert.Null(await vdb.OutboxMessages.FirstOrDefaultAsync(m => m.Id == outbox.Id));

        var archivedInbox = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "InboxMessages")
            .ToListAsync();
        var archivedOutbox = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "OutboxMessages")
            .ToListAsync();
        Assert.NotNull(archivedInbox.SingleOrDefault(r => r.Payload.Contains(inbox.Id.ToString())));
        Assert.NotNull(archivedOutbox.SingleOrDefault(r => r.Payload.Contains(outbox.Id.ToString())));
    }

    private async Task RunRetentionAsync()
    {
        var service = _factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<LedgerRetentionService>().Single();
        await service.RunOnceAsync(CancellationToken.None);
    }
}