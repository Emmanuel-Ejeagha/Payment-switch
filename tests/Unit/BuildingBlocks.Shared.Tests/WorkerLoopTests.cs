using BuildingBlocks.Shared.BackgroundServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildingBlocks.Shared.Tests;

public class WorkerLoopTests
{
    [Fact]
    public async Task RunAsync_StopsScheduling_AndRunsFinalDrainOnShutdown()
    {
        var calls = new List<bool>();
        var cts = new CancellationTokenSource();

        var run = WorkerLoop.RunAsync(
            ct =>
            {
                lock (calls)
                    calls.Add(ct.IsCancellationRequested);
                return Task.CompletedTask;
            },
            TimeSpan.FromDays(1),
            NullLogger.Instance,
            cts.Token,
            "test worker");

        await Task.Delay(100);
        cts.Cancel();
        await run;

        // One scheduled pump pass (token not yet cancelled) + one shutdown drain
        // pass (WorkerLoop passes a fresh, non-cancelled token so the in-flight
        // batch completes). Both run with a usable token.
        Assert.Equal(2, calls.Count);
        Assert.All(calls, cancelled => Assert.False(cancelled));
    }

    [Fact]
    public async Task RunAsync_CancellationMidWork_ExitsCleanly_AndStillDrains()
    {
        var processed = 0;
        var cts = new CancellationTokenSource();

        var run = WorkerLoop.RunAsync(
            ct =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);
                Interlocked.Increment(ref processed);
                return Task.CompletedTask;
            },
            TimeSpan.FromDays(1),
            NullLogger.Instance,
            cts.Token,
            "test worker");

        await Task.Delay(100);
        cts.Cancel();
        await run;

        // The cancelled pump pass aborts without error noise, then the drain pass
        // runs one more bounded iteration with a usable token.
        Assert.Equal(2, processed);
    }
}