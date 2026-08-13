using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Shared.Messaging;
using Ledger.Infrastructure.Messaging;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// TASK-028 acceptance: a message published to the broker carrying a W3C
/// traceparent header is resumed by the Ledger consumer as a child activity of
/// the same trace, so one trace spans HTTP → outbox → broker → consumer → DB.
/// </summary>
public class TracePropagationTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public TracePropagationTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Consumer_ResumesTheProducersTrace_FromMessageHeaders()
    {
        var consumerActivities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "PaymentSwitch",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                if (activity.Kind == ActivityKind.Consumer)
                    consumerActivities.Enqueue(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        // The factory starts a fresh broker + API host per test class; wait until
        // the consumer has declared and is consuming its queue so the published
        // message is routable (mandatory=false drops unroutable publishes).
        await WaitUntilConsumerReadyAsync();

        var merchantId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        var traceId = Guid.NewGuid().ToString("N");
        var parentSpanId = Guid.NewGuid().ToString("N")[..16];
        var traceParent = $"00-{traceId}-{parentSpanId}-01";
        var payload = JsonSerializer.Serialize(new PaymentAuthorizedEvent(
            Guid.NewGuid(), merchantId, new MoneyPayload(5000, "USD"), "auth-code", "gateway-ref"));

        await PublishToPaymentEventsAsync(messageId, payload, traceParent);

        await WaitUntilAsync(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ProcessedAt != null);
        });

        Assert.Contains(consumerActivities, a => a.TraceId.ToString() == traceId);
        Assert.Contains(
            consumerActivities,
            a => a.TraceId.ToString() == traceId && a.ParentId is not null && a.ParentId.Contains(parentSpanId));
    }

    private async Task WaitUntilConsumerReadyAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMQSettings>>();
        var factory = new ConnectionFactory
        {
            HostName = settings.Value.HostName,
            Port = settings.Value.Port,
            UserName = settings.Value.UserName,
            Password = settings.Value.Password
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var deadline = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var queueInfo = await channel.QueueDeclarePassiveAsync("ledger.payment.events");
                if (queueInfo.ConsumerCount > 0)
                    return;
            }
            catch (Exception)
            {
                // Queue not declared yet; keep polling.
            }

            await Task.Delay(250);
        }

        Assert.Fail("Ledger consumer never became ready to consume.");
    }

    private async Task PublishToPaymentEventsAsync(string messageId, string payload, string traceParent)
    {
        using var scope = _factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMQSettings>>();
        var factory = new ConnectionFactory
        {
            HostName = settings.Value.HostName,
            Port = settings.Value.Port,
            UserName = settings.Value.UserName,
            Password = settings.Value.Password
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = "corr-" + messageId,
            Headers = new Dictionary<string, object?>
            {
                [RabbitMqTracing.TraceParentHeader] = Encoding.UTF8.GetBytes(traceParent)
            }
        };
        await channel.BasicPublishAsync(
            exchange: "payment.events",
            routingKey: "PaymentAuthorizedDomainEvent",
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payload));
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
                return;
            await Task.Delay(500);
        }

        Assert.Fail("Timed out waiting for consumer to process the traced message.");
    }
}
