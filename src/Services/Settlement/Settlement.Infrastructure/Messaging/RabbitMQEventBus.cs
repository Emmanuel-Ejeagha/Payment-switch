using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Settlement.Application.Interfaces;

namespace Settlement.Infrastructure.Messaging;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private bool _disposed;

    public RabbitMQEventBus(IOptions<RabbitMQSettings> settings)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Value.HostName,
            UserName = settings.Value.UserName,
            Password = settings.Value.Password
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: settings.Value.ExchangeName,
            type: ExchangeType.Topic,
            durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(payload);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await _channel.BasicPublishAsync(
            exchange: "settlement.events",
            routingKey: eventType,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}