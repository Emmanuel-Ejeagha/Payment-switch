using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingBlocks.Shared.Messaging;

/// <summary>
/// Owns a single RabbitMQ connection per service and hands out pooled channels,
/// so the outbox publisher no longer opens a fresh connection per poll cycle.
///
/// Recovery is deliberate rather than the client's automatic recovery: when the
/// broker goes away the shutdown handler discards every pooled channel and the
/// next <see cref="GetChannelAsync"/> call transparently recreates the connection
/// and re-declares the topology through <paramref name="declareTopologyAsync"/>.
/// </summary>
public sealed class RabbitMqChannelPool : IDisposable, IAsyncDisposable
{
    private readonly ConcurrentBag<IChannel> _channels = new();
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private readonly Func<ConnectionFactory> _connectionFactory;
    private readonly Func<IChannel, Task> _declareTopologyAsync;
    private readonly ILogger _logger;
    private IConnection? _connection;
    private bool _disposed;

    /// <summary>Number of distinct RabbitMQ connections this pool has created (for tests/metrics).</summary>
    public int ConnectionsCreated { get; private set; }

    /// <summary>Number of channels this pool has created (for tests/metrics).</summary>
    public int ChannelsCreated { get; private set; }

    public RabbitMqChannelPool(
        Func<ConnectionFactory> connectionFactory,
        Func<IChannel, Task> declareTopologyAsync,
        ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _declareTopologyAsync = declareTopologyAsync;
        _logger = logger;
    }

    /// <summary>
    /// Returns an open channel with the topology already declared. When the broker
    /// is unreachable the caller is expected to retry (the outbox publisher does),
    /// exactly as the pre-pool bus behaved.
    /// </summary>
    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_channels.TryTake(out var pooled) && pooled.IsOpen)
                return pooled;

            if (pooled is not null)
                await DiscardAsync(pooled);

            var connection = await GetOrCreateConnectionAsync(cancellationToken);
            if (connection is { IsOpen: true })
            {
                var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                ChannelsCreated++;
                await _declareTopologyAsync(channel);
                return channel;
            }
        }

        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>Returns a channel to the pool for reuse, disposing it if it died mid-use.</summary>
    public async Task ReturnAsync(IChannel channel)
    {
        if (channel.IsOpen)
        {
            _channels.Add(channel);
        }
        else
        {
            await DiscardAsync(channel);
        }
    }

    private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
    {
        var current = _connection;
        if (current is { IsOpen: true })
            return current;

        await _connectGate.WaitAsync(cancellationToken);
        try
        {
            current = _connection;
            if (current is { IsOpen: true })
                return current;

            if (current is not null)
            {
                await DiscardAsync(current);
                _connection = null;
            }

            var factory = _connectionFactory();
            // Recovery is handled here (channels discarded on shutdown, connection
            // recreated on demand) so topology re-declaration is deterministic.
            factory.AutomaticRecoveryEnabled = false;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(5);

            var connection = await factory.CreateConnectionAsync(cancellationToken);
            connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            _connection = connection;
            ConnectionsCreated++;
            return connection;
        }
        finally
        {
            _connectGate.Release();
        }
    }

    private async Task OnConnectionShutdownAsync(object? sender, ShutdownEventArgs eventArgs)
    {
        // Reply code 200 / Application initiator means our own CloseAsync, which
        // runs during Dispose; there is nothing to recover.
        if (eventArgs.Initiator == ShutdownInitiator.Application)
            return;

        // A stale connection that shut down after being replaced must not touch
        // channels that belong to the current connection.
        if (!ReferenceEquals(sender, _connection))
            return;

        _logger.LogWarning("RabbitMQ connection shut down ({ReplyCode} {ReplyText}); discarding pooled channels",
            eventArgs.ReplyCode, eventArgs.ReplyText);

        while (_channels.TryTake(out var channel))
        {
            await DiscardAsync(channel);
        }
    }

    private async Task DiscardAsync(IChannel channel)
    {
        try
        {
            await channel.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose RabbitMQ channel during recovery");
        }
    }

    private async Task DiscardAsync(IConnection connection)
    {
        try
        {
            await connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose RabbitMQ connection during recovery");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        while (_channels.TryTake(out var channel))
        {
            try
            {
                channel.Dispose();
            }
            catch
            {
                // Best-effort teardown at shutdown.
            }
        }

        if (_connection is not null)
        {
            try
            {
                _connection.Dispose();
            }
            catch
            {
                // Best-effort teardown at shutdown.
            }
            _connection = null;
        }

        _connectGate.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }
}
