using BuildingBlocks.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Payment.Domain;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Outbox;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure.Tests.Outbox;

public class OutboxInterceptorTests
{
    [Fact]
    public async Task SavingChanges_WhenCorrelationIdSet_ShouldStampOutboxMessageWithCorrelationId()
    {
        var provider = new CorrelationIdProvider();
        provider.Set("corr-123");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new OutboxInterceptor(provider))
            .Options;

        await using var context = new AppDbContext(options);
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("key"), PaymentMethod.Card);
        context.PaymentIntents.Add(intent);

        await context.SaveChangesAsync();

        var outbox = await context.OutboxMessages.ToListAsync();
        var message = Assert.Single(outbox);
        Assert.Equal("PaymentIntentCreatedDomainEvent", message.EventType);
        Assert.Equal($"corr-123:{intent.Id}:PaymentIntentCreatedDomainEvent", message.CorrelationId);
    }

    [Fact]
    public async Task SavingChanges_WhenNoCorrelationId_ShouldLeaveOutboxMessageCorrelationIdNull()
    {
        var provider = new CorrelationIdProvider();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new OutboxInterceptor(provider))
            .Options;

        await using var context = new AppDbContext(options);
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("key"), PaymentMethod.Card);
        context.PaymentIntents.Add(intent);

        await context.SaveChangesAsync();

        var outbox = await context.OutboxMessages.ToListAsync();
        var message = Assert.Single(outbox);
        Assert.Null(message.CorrelationId);
    }

    [Fact]
    public async Task SavingChanges_AuthorizePlusCaptureInOneSave_WritesDistinctCorrelations()
    {
        // Auto-capture flushes Authorized + Captured together. Sharing one
        // correlation made the capture leg violate the unique journal index
        // downstream (funds stuck reserved); each row must carry its own.
        var provider = new CorrelationIdProvider();
        provider.Set("corr-auto");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new OutboxInterceptor(provider))
            .Options;

        await using var context = new AppDbContext(options);
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("key"), PaymentMethod.Card);
        intent.Authorize(new AuthorizationCode("AUTH-1"), new GatewayReference("GW-1"), "auth-key");
        intent.Capture(null, "cap-key");
        context.PaymentIntents.Add(intent);

        await context.SaveChangesAsync();

        var outbox = await context.OutboxMessages
            .Where(m => m.EventType == "PaymentAuthorizedDomainEvent" || m.EventType == "PaymentCapturedDomainEvent")
            .ToListAsync();
        Assert.Equal(2, outbox.Count);
        Assert.All(outbox, m => Assert.StartsWith("corr-auto:", m.CorrelationId));
        Assert.Equal(2, outbox.Select(m => m.CorrelationId).Distinct().Count());
    }

    [Fact]
    public async Task SavingChanges_ForPaymentIntent_ShouldAlsoEnqueueWebhookEvent()
    {
        var provider = new CorrelationIdProvider();
        provider.Set("corr-wh");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new OutboxInterceptor(provider))
            .Options;

        await using var context = new AppDbContext(options);
        var merchantId = Guid.NewGuid();
        var intent = new PaymentIntent(Guid.NewGuid(), merchantId, new Money(100, "USD"), new IdempotencyKey("key"), PaymentMethod.Card);
        context.PaymentIntents.Add(intent);

        await context.SaveChangesAsync();

        var webhookEvents = await context.WebhookEvents.ToListAsync();
        var webhookEvent = Assert.Single(webhookEvents);
        Assert.Equal("PaymentIntentCreatedDomainEvent", webhookEvent.EventType);
        Assert.Equal(merchantId, webhookEvent.MerchantId);
        Assert.Equal(WebhookEvent.StatusPending, webhookEvent.Status);
        Assert.Equal("corr-wh", webhookEvent.CorrelationId);
    }
}
