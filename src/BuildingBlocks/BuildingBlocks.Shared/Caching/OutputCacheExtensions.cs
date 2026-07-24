using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Shared.Caching;

public static class OutputCacheExtensions
{
    public static IServiceCollection AddPaymentSwitchOutputCache(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(30);
            options.AddBasePolicy(builder => builder.Tag("default"));
            options.AddPolicy("CacheById", builder =>
                builder.Expire(TimeSpan.FromSeconds(60)).Tag("by-id"));
        });

        return services;
    }

    public static IApplicationBuilder UsePaymentSwitchOutputCache(this IApplicationBuilder app)
    {
        return app.UseOutputCache();
    }
}
