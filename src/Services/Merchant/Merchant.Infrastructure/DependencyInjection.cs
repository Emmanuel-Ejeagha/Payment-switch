using BuildingBlocks.Shared.Configuration;
using Merchant.Infrastructure.Messaging;
using Merchant.Infrastructure.Outbox;
using Merchant.Infrastructure.Persistence;
using Merchant.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Merchant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMerchantInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("MerchantDb"), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
            })
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddScoped<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}