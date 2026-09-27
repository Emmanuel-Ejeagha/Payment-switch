using BuildingBlocks.Shared.Messaging;
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
        // Config-driven OTLP endpoint: app config wins, then the standard
        // OTEL_EXPORTER_OTLP_ENDPOINT environment variable, then the compose
        // default. Previously hardcoded to http://jaeger:4317 (TASK-028).
        var otlpEndpoint = builder.Configuration["Otlp:Endpoint"]
            ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? "http://jaeger:4317";

        return builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddGrpcClientInstrumentation();
                tracing.AddSource(RabbitMqTracing.Source.Name);
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddMeter("PaymentSwitch");
            });
    }
}