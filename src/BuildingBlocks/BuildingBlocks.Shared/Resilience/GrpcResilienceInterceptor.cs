using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Shared.Resilience;

public class GrpcResilienceInterceptor : Interceptor
{
    private readonly GrpcResilienceOptions _options;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly ILogger<GrpcResilienceInterceptor> _logger;

    public GrpcResilienceInterceptor(GrpcResilienceOptions options, ILogger<GrpcResilienceInterceptor> logger)
    {
        _options = options;
        _logger = logger;
        _circuitBreaker = new CircuitBreaker(options.CircuitBreakerFailureThreshold, options.CircuitBreakerOpenDuration);
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        var holder = new CallHolder<TResponse>();
        var responseTask = InvokeAsync(request, context, continuation, holder);

        return new AsyncUnaryCall<TResponse>(
            responseTask,
            (state) => holder.Latest?.ResponseHeadersAsync ?? Task.FromResult(new Metadata()),
            (state) => holder.Latest?.GetStatus() ?? new Status(StatusCode.Unavailable, "Call did not complete."),
            (state) => holder.Latest?.GetTrailers() ?? new Metadata(),
            (state) => holder.Latest?.Dispose(),
            holder);
    }

    private async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation,
        CallHolder<TResponse> holder)
        where TRequest : class
        where TResponse : class
    {
        var attempt = 0;

        while (true)
        {
            if (!_circuitBreaker.CanProceed())
            {
                _logger.LogWarning("gRPC call {Method} rejected: circuit breaker open.", context.Method.FullName);
                throw new RpcException(new Status(StatusCode.Unavailable, "gRPC circuit breaker is open."));
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(context.Options.CancellationToken);
            timeoutCts.CancelAfter(_options.PerAttemptTimeout);

            var callOptions = context.Options.WithCancellationToken(timeoutCts.Token);
            var attemptContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, callOptions);
            var call = continuation(request, attemptContext);
            holder.Latest = call;

            try
            {
                var response = await call.ResponseAsync.ConfigureAwait(false);
                _circuitBreaker.RecordSuccess();
                return response;
            }
            catch (RpcException ex) when (_options.IsRetryable(ex.StatusCode))
            {
                call.Dispose();
                _circuitBreaker.RecordFailure();

                attempt++;
                if (attempt >= _options.MaxRetryAttempts)
                {
                    throw;
                }

                var delay = _options.GetBackoffDelay(attempt);
                _logger.LogWarning(
                    "gRPC call {Method} failed with status {StatusCode}; retrying attempt {Attempt}/{Max} after {DelayMs}ms.",
                    context.Method.FullName, ex.StatusCode, attempt, _options.MaxRetryAttempts, delay.TotalMilliseconds);

                await Task.Delay(delay, context.Options.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !context.Options.CancellationToken.IsCancellationRequested)
            {
                call.Dispose();
                _circuitBreaker.RecordFailure();

                attempt++;
                if (attempt >= _options.MaxRetryAttempts)
                {
                    throw new RpcException(new Status(StatusCode.DeadlineExceeded, $"gRPC call {context.Method.FullName} timed out after {_options.PerAttemptTimeout.TotalSeconds}s."));
                }

                var delay = _options.GetBackoffDelay(attempt);
                _logger.LogWarning(
                    "gRPC call {Method} timed out; retrying attempt {Attempt}/{Max} after {DelayMs}ms.",
                    context.Method.FullName, attempt, _options.MaxRetryAttempts, delay.TotalMilliseconds);

                await Task.Delay(delay, context.Options.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class CallHolder<TResponse>
    {
        public AsyncUnaryCall<TResponse>? Latest { get; set; }
    }
}
