using System.Diagnostics;
using System.Text;

namespace BuildingBlocks.Shared.Messaging;

/// <summary>
/// Propagates W3C trace context over RabbitMQ so a single trace spans
/// HTTP → outbox → broker → consumer → DB across services.
///
/// The producer injects <c>traceparent</c>/<c>tracestate</c> into the AMQP
/// message headers; the consumer extracts them and resumes the trace as a
/// child activity (TASK-028).
/// </summary>
public static class RabbitMqTracing
{
    public const string TraceParentHeader = "traceparent";
    public const string TraceStateHeader = "tracestate";

    /// <summary>Shared source, registered via <c>AddSource</c> in <c>AddPaymentSwitchObservability</c>.</summary>
    public static readonly ActivitySource Source = new("PaymentSwitch", "1.0.0");

    /// <summary>
    /// Writes the current <see cref="Activity"/>'s trace context into the message
    /// headers. No-op when there is no ambient activity (e.g. a publish that is
    /// not part of a request flow).
    /// </summary>
    public static void InjectTracingContext(IDictionary<string, object?> headers)
    {
        var activity = Activity.Current;
        if (activity is null || activity.TraceId == default)
            return;

        var traceParent = $"{activity.TraceId}-{activity.SpanId}-{(activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00")}";
        headers[TraceParentHeader] = traceParent;
        if (!string.IsNullOrEmpty(activity.TraceStateString))
            headers[TraceStateHeader] = activity.TraceStateString;
    }

    /// <summary>
    /// Reads the parent <see cref="ActivityContext"/> from message headers, or
    /// <c>null</c> when no (valid) trace context travelled with the message.
    /// </summary>
    public static ActivityContext? ExtractActivityContext(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(TraceParentHeader, out var value))
            return null;

        var traceParent = value switch
        {
            string s => s,
            byte[] b => Encoding.UTF8.GetString(b),
            _ => value?.ToString()
        };

        if (string.IsNullOrWhiteSpace(traceParent))
            return null;

        return ActivityContext.TryParse(traceParent, null, out var context) ? context : null;
    }

    /// <summary>Starts the producer span for an outbox publish, linked to the HTTP span that enqueued it.</summary>
    public static Activity? StartProducerActivity(string eventType, ActivityContext? parentContext)
    {
        return Source.StartActivity(
            $"outbox.publish {eventType}",
            ActivityKind.Producer,
            parentContext ?? default,
            tags: new Dictionary<string, object?>
            {
                ["messaging.rabbitmq.routing_key"] = eventType
            });
    }

    /// <summary>Starts the consumer span, resuming the trace from the message headers.</summary>
    public static Activity? StartConsumerActivity(string eventType, ActivityContext? parentContext)
    {
        return Source.StartActivity(
            $"consume {eventType}",
            ActivityKind.Consumer,
            parentContext ?? default,
            tags: new Dictionary<string, object?>
            {
                ["messaging.rabbitmq.routing_key"] = eventType
            });
    }
}
