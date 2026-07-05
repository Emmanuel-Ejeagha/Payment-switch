using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Shared;

public static class OpenTelemetryExtensions
{
    public static OpenTelemetryBuilder AddPaymentSwitchObservability(this IHostApplicationBuilder builder, string serviceName)
    {
        return builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://jaeger:4317");
                });
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
            });
    }
}