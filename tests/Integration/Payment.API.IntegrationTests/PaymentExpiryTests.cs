using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.API.IntegrationTests;

/// <summary>
/// TASK-018: the expiry sweep must find in-flight intents past the TTL, expire
/// them idempotently, and persist the Expired status so later authorize/capture
/// attempts are rejected (the state machine guards those transitions).
/// </summary>
public class PaymentExpiryTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;

    public PaymentExpiryTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExpirableBatch_ReturnsOnlyStaleInFlightIntents()
    {
        var staleId = Guid.NewGuid();
        var freshId = Guid.NewGuid();
        var capturedId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PaymentIntents.Add(new PaymentIntent(staleId, Guid.NewGuid(),
                new Money(100, "USD"), new IdempotencyKey("exp-key-stale"), PaymentMethod.Card));
            db.PaymentIntents.Add(new PaymentIntent(freshId, Guid.NewGuid(),
                new Money(100, "USD"), new IdempotencyKey("exp-key-fresh"), PaymentMethod.Card));
            var captured = new PaymentIntent(capturedId, Guid.NewGuid(),
                new Money(100, "USD"), new IdempotencyKey("exp-key-captured"), PaymentMethod.Card);
            captured.Authorize(new AuthorizationCode("AUTH"), new GatewayReference("GW"));
            captured.Capture(new Money(100, "USD"));
            db.PaymentIntents.Add(captured);

            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"PaymentIntents\" SET \"CreatedAt\" = {0}, \"UpdatedAt\" = {0} WHERE \"Id\" = {1}",
                now.AddHours(-2), staleId);
        }

        using var queryScope = _factory.Services.CreateScope();
        var repository = queryScope.ServiceProvider
            .GetRequiredService<Payment.Application.Interfaces.IPaymentIntentRepository>();

        var batch = await repository.GetExpirableBatchAsync(now.AddHours(-1), 50);

        Assert.Contains(batch, p => p.Id == staleId);
        Assert.DoesNotContain(batch, p => p.Id == freshId);
        Assert.DoesNotContain(batch, p => p.Id == capturedId);
    }

    [Fact]
    public async Task ExpiredIntent_PersistsExpiredStatus_AndIsNotFoundInNextSweep()
    {
        var intentId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PaymentIntents.Add(new PaymentIntent(intentId, merchantId,
                new Money(100, "USD"), new IdempotencyKey("exp-key-roundtrip"), PaymentMethod.Card));
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"PaymentIntents\" SET \"CreatedAt\" = {0}, \"UpdatedAt\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddHours(-2), intentId);
        }

        PaymentIntent intent;
        using (var queryScope = _factory.Services.CreateScope())
        {
            var repository = queryScope.ServiceProvider
                .GetRequiredService<Payment.Application.Interfaces.IPaymentIntentRepository>();
            var batch = await repository.GetExpirableBatchAsync(DateTime.UtcNow.AddHours(-1), 50);
            intent = batch.Single(p => p.Id == intentId);
        }

        intent.Expire();

        using (var saveScope = _factory.Services.CreateScope())
        {
            var db = saveScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.PaymentIntents.Update(intent);
            await db.SaveChangesAsync();
        }

        // Round-trips as Expired through EF's status converter.
        using (var readScope = _factory.Services.CreateScope())
        {
            var db = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await db.PaymentIntents.AsNoTracking().SingleAsync(p => p.Id == intentId);
            Assert.Equal(PaymentStatus.Expired, persisted.Status);
        }

        // The next sweep must not pick it up again (idempotent, terminal).
        using (var nextScope = _factory.Services.CreateScope())
        {
            var repository = nextScope.ServiceProvider
                .GetRequiredService<Payment.Application.Interfaces.IPaymentIntentRepository>();
            var nextBatch = await repository.GetExpirableBatchAsync(DateTime.UtcNow.AddHours(-1), 50);
            Assert.DoesNotContain(nextBatch, p => p.Id == intentId);
        }
    }
}