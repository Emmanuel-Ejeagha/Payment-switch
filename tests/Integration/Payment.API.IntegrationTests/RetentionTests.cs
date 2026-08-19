using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Outbox;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Retention;

namespace Payment.API.IntegrationTests;

/// <summary>
/// TASK-042: the Payment retention sweep archives expired payment intents (with
/// their transaction history) and processed outbox messages into
/// <c>ArchivedRecords</c> instead of discarding them; fresh records are untouched.
/// </summary>
public class RetentionTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;

    public RetentionTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PaymentIntent_Expired_IsArchivedAndRemoved_FreshStays()
    {
        var oldId = Guid.NewGuid();
        var freshId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PaymentIntents.Add(new PaymentIntent(oldId, merchantId,
                new Money(5000, "USD"), new IdempotencyKey("ret-key-old"), PaymentMethod.Card));
            db.PaymentIntents.Add(new PaymentIntent(freshId, merchantId,
                new Money(100, "USD"), new IdempotencyKey("ret-key-fresh"), PaymentMethod.Card));
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"PaymentIntents\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddYears(-8), oldId);
        }

        await RunRetentionAsync();

        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await vdb.PaymentIntents.FirstOrDefaultAsync(p => p.Id == oldId));
        Assert.NotNull(await vdb.PaymentIntents.FirstOrDefaultAsync(p => p.Id == freshId));

        var archivedIntents = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "PaymentIntents")
            .ToListAsync();
        var archive = archivedIntents.SingleOrDefault(r => r.Payload.Contains(oldId.ToString()));
        Assert.NotNull(archive);
        using (var doc = System.Text.Json.JsonDocument.Parse(archive!.Payload))
        {
            var amount = doc.RootElement.GetProperty("Amount");
            Assert.Equal(5000, amount.GetProperty("Amount").GetInt64());
            Assert.Equal("USD", amount.GetProperty("Currency").GetString());
        }
    }

    [Fact]
    public async Task OutboxMessage_ProcessedAndExpired_IsArchivedAndRemoved()
    {
        OutboxMessage old;
        OutboxMessage recent;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            old = new OutboxMessage("PaymentAuthorizedEvent", """{"amount":5000}""");
            old.MarkAsProcessed();
            recent = new OutboxMessage("PaymentAuthorizedEvent", """{"amount":5000}""");
            recent.MarkAsProcessed();
            db.OutboxMessages.Add(old);
            db.OutboxMessages.Add(recent);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"OutboxMessages\" SET \"OccurredOn\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddDays(-30), old.Id);
        }

        await RunRetentionAsync();

        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await vdb.OutboxMessages.FirstOrDefaultAsync(m => m.Id == old.Id));
        Assert.NotNull(await vdb.OutboxMessages.FirstOrDefaultAsync(m => m.Id == recent.Id));

        var archivedOutbox = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "OutboxMessages")
            .ToListAsync();
        Assert.NotNull(archivedOutbox.SingleOrDefault(r => r.Payload.Contains(old.Id.ToString())));
    }

    private async Task RunRetentionAsync()
    {
        var service = _factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<PaymentRetentionService>().Single();
        await service.RunOnceAsync(CancellationToken.None);
    }
}