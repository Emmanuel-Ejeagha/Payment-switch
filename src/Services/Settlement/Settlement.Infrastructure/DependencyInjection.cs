using BuildingBlocks.Shared.Configuration;
using Settlement.Application.Interfaces;
using Settlement.Infrastructure.Outbox;
using Settlement.Infrastructure.Persistence;
using Settlement.Infrastructure.Persistence.Repositories;
using Settlement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Settlement.Infrastructure.Messaging;
using PaymentSwitch.Protos.Ledger;

namespace Settlement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSettlementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("SettlementDb"), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
            })
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<ISettlementBatchRepository, SettlementBatchRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILedgerService, GrpcLedgerService>();

        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddScoped<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        services.AddGrpcClient<LedgerService.LedgerServiceClient>(o =>
        {
            o.Address = new Uri("http://ledger-api:8080");
        });

        return services;
    }
}