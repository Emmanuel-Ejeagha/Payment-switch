using BuildingBlocks.Shared.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Shared.Tests;

public class HealthCheckExtensionsTests
{
    [Fact]
    public async Task RabbitMqCheck_ReturnsUnhealthy_Fast_WhenBrokerUnreachable()
    {
        var freePort = GetFreePort();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMQ:HostName"] = "127.0.0.1",
                ["RabbitMQ:Port"] = freePort.ToString()
            })
            .Build();

        using var sp = new ServiceCollection()
            .AddLogging()
            .AddHealthChecks()
            .AddRabbitMqHealthCheck(config)
            .Services
            .BuildServiceProvider();
        var healthCheckService = sp.GetRequiredService<HealthCheckService>();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));
        sw.Stop();

        // Readiness reports the broker as down, but must answer promptly instead
        // of blocking on a default TCP connect timeout.
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(3), $"health check took {sw.Elapsed}");
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}