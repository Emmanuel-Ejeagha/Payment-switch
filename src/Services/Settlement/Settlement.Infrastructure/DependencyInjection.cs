using Settlement.Application.Interfaces;
using Settlement.Infrastructure.Outbox;
using Settlement.Infrastructure.Persistence;
using Settlement.Infrastructure.Persistence.Repositories;
using Settlement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Settlement.Infrastructure.Messaging;

namespace Settlement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSettlementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("SettlementDb"))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<ISettlementBatchRepository, SettlementBatchRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILedgerService, MockLedgerService>();

        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
        services.AddScoped<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}