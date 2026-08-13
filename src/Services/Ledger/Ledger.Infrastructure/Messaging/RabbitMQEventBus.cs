using System.Text;
using BuildingBlocks.Shared.Messaging;
using Ledger.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Ledger.Infrastructure.Messaging;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly RabbitMqChannelPool _channelPool;
    private bool _disposed;

    public RabbitMQEventBus(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQEventBus> logger)
    {
        _settings = settings.Value;
        _channelPool = new RabbitMqChannelPool(
            () => new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                ClientProvidedName = "ledger-bus"
            },
            channel => channel.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true),
            logger);
    }

    public async Task PublishAsync(string eventType, string payload, string? messageId = null, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = correlationId
        };

        var channel = await _channelPool.GetChannelAsync(cancellationToken);
        try
        {
            await channel.BasicPublishAsync(
                exchange: _settings.ExchangeName,
                routingKey: eventType,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            await _channelPool.ReturnAsync(channel);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channelPool.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
