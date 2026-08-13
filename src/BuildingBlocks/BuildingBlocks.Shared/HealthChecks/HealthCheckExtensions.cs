using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Shared.HealthChecks;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddPaymentSwitchHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks();
    }

    /// <summary>
    /// Readiness check for the RabbitMQ broker. Bounded by a 3s connect timeout
    /// so <c>/health/ready</c> stays responsive when the broker is down or
    /// unreachable (a default <see cref="System.Net.Sockets.TcpClient"/> connect
    /// to an unresponsive host can otherwise block for tens of seconds).
    /// Readiness reflects dependency health, but the service itself keeps running
    /// — producers/consumers recover lazily or via their reconnect loops.
    /// </summary>
    public static IHealthChecksBuilder AddRabbitMqHealthCheck(this IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:HostName"] ?? "localhost";
        var port = int.TryParse(configuration["RabbitMQ:Port"], out var p) ? p : 5672;

        return builder.AddAsyncCheck("rabbitmq", async cancellationToken =>
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                using var tcp = new System.Net.Sockets.TcpClient();
                await tcp.ConnectAsync(host, port, timeout.Token);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ unreachable", ex);
            }
        }, tags: ["ready"]);
    }

    public static WebApplication MapPaymentSwitchHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteMinimalResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteDetailedResponse
        });

        return app;
    }

    private static async Task WriteMinimalResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        await context.Response.WriteAsync(
            $"{{\"status\":\"{report.Status}\"}}");
    }

    private static async Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(json);
    }
}
