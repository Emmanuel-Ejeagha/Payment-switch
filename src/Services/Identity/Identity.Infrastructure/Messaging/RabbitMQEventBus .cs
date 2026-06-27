using System.Text;
using Identity.Application.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Identity.Infrastructure.Messaging;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMQEventBus(IOptions<RabbitMQSettings> settings)
    {
        _settings = settings.Value;
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
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
            exchange: _settings.ExchangeName,
            routingKey: eventType,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}