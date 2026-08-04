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
        Assert.Equal("corr-123", message.CorrelationId);
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
