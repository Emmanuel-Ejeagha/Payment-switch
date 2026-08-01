using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Shared.Resilience;

public static class GrpcClientBuilderExtensions
{
    public static IHttpClientBuilder AddGrpcResilienceInterceptor(
        this IHttpClientBuilder builder,
        Action<GrpcResilienceOptions>? configure = null)
    {
        return builder.AddInterceptor((sp) =>
        {
            var options = sp.GetRequiredService<IOptions<GrpcResilienceOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<GrpcResilienceInterceptor>>();

            if (configure is null)
            {
                return new GrpcResilienceInterceptor(options, logger);
            }

            var copy = new GrpcResilienceOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                PerAttemptTimeout = options.PerAttemptTimeout,
                BaseDelay = options.BaseDelay,
                MaxDelay = options.MaxDelay,
                CircuitBreakerFailureThreshold = options.CircuitBreakerFailureThreshold,
                CircuitBreakerOpenDuration = options.CircuitBreakerOpenDuration,
                RetryableStatusCodes = options.RetryableStatusCodes,
            };
            configure(copy);
            return new GrpcResilienceInterceptor(copy, logger);
        });
    }
}
