using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace BuildingBlocks.Shared.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Per-client rate limiting (Step 7.4): <c>Default</c> and <c>Strict</c>
    /// are partitioned policies — every IP / authenticated user gets its own
    /// fixed window — and the global limiter is partitioned the same way.
    /// Previously both were single global buckets shared by all clients.
    /// <c>Strict</c> guards auth and key-management endpoints (Identity
    /// Auth/Admin, Merchant onboarding/keys); API-key <i>failures</i> are
    /// throttled separately inside <c>SecretKeyAuthMiddleware</c> because the
    /// middleware short-circuits with 401 before endpoint policies engage.
    /// Rejections are 429. Buckets are per-instance memory: with N replicas
    /// each client effectively gets N× the budget (see ROADMAP G39 for the
    /// Redis-backed future); the per-client partition still bounds any single
    /// source, which is what stops brute force and per-client DoS.
    /// </summary>
    public static IServiceCollection AddPaymentSwitchRateLimiting(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        var settings = new RateLimitSettings();
        configuration?.GetSection(RateLimitSettings.SectionName).Bind(settings);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("Default", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    RateLimitPartitionKey.GetKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.DefaultPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.DefaultWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.DefaultQueueLimit
                    }));

            options.AddPolicy("Strict", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    RateLimitPartitionKey.GetKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.StrictPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.StrictWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    RateLimitPartitionKey.GetKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.DefaultPermitLimit,
                        Window = TimeSpan.FromSeconds(settings.DefaultWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.DefaultQueueLimit
                    }));
        });

        return services;
    }
}
