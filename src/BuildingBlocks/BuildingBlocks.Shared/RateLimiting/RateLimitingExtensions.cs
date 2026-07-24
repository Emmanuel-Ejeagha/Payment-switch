using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace BuildingBlocks.Shared.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddPaymentSwitchRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("Default", config =>
            {
                config.PermitLimit = 100;
                config.Window = TimeSpan.FromSeconds(10);
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 5;
            });

            options.AddFixedWindowLimiter("Strict", config =>
            {
                config.PermitLimit = 20;
                config.Window = TimeSpan.FromSeconds(10);
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });
        });

        return services;
    }
}
