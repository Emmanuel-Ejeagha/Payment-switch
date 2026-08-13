using System.Diagnostics;
using System.Text;
using BuildingBlocks.Shared.Messaging;

namespace BuildingBlocks.Shared.Tests;

public class RabbitMqTracingTests : IDisposable
{
    private readonly ActivityListener _listener;

    public RabbitMqTracingTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "PaymentSwitch",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Fact]
    public void InjectTracingContext_WhenNoAmbientActivity_AddsNothing()
    {
        var headers = new Dictionary<string, object?>();
        RabbitMqTracing.InjectTracingContext(headers);
        Assert.Empty(headers);
    }

    [Fact]
    public void InjectThenExtract_RoundTripsTheTraceId()
    {
        var activity = new Activity("http.request").Start();
        try
        {
            var headers = new Dictionary<string, object?>();
            RabbitMqTracing.InjectTracingContext(headers);

            var context = RabbitMqTracing.ExtractActivityContext(headers);

            Assert.NotNull(context);
            Assert.Equal(activity.TraceId, context!.Value.TraceId);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void Extract_WhenHeaderIsByteArray_RoundTrips()
    {
        var activity = new Activity("http.request").Start();
        try
        {
            var headers = new Dictionary<string, object?>
            {
                [RabbitMqTracing.TraceParentHeader] = Encoding.UTF8.GetBytes($"00-{activity.TraceId}-{activity.SpanId}-01")
            };

            var context = RabbitMqTracing.ExtractActivityContext(headers);

            Assert.NotNull(context);
            Assert.Equal(activity.TraceId, context!.Value.TraceId);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void ParseTraceParent_WhenMissingOrInvalid_ReturnsNull()
    {
        Assert.Null(RabbitMqTracing.ParseTraceParent(null));
        Assert.Null(RabbitMqTracing.ParseTraceParent(""));
        Assert.Null(RabbitMqTracing.ParseTraceParent("not-a-traceparent"));
    }

    [Fact]
    public void CurrentTraceParent_WhenNoAmbientActivity_ReturnsNull()
    {
        Assert.Null(RabbitMqTracing.CurrentTraceParent());
    }

    [Fact]
    public void CurrentTraceParent_WhenActive_ReturnsVersionedString()
    {
        var activity = new Activity("http.request").Start();
        try
        {
            var traceParent = RabbitMqTracing.CurrentTraceParent();
            Assert.NotNull(traceParent);
            Assert.StartsWith("00-", traceParent);
            Assert.Equal(activity.TraceId.ToString(), traceParent!.Split('-')[1]);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void StartConsumerActivity_IsChildOfTheParsedParent()
    {
        var root = new Activity("http.request").Start();
        try
        {
            var parent = RabbitMqTracing.ParseTraceParent(RabbitMqTracing.CurrentTraceParent());
            using var consumerActivity = RabbitMqTracing.StartConsumerActivity("PaymentAuthorizedDomainEvent", parent);

            Assert.NotNull(consumerActivity);
            Assert.Equal(root.TraceId, consumerActivity!.TraceId);
            Assert.StartsWith($"00-{root.TraceId}-", consumerActivity.ParentId);
        }
        finally
        {
            root.Stop();
        }
    }
}