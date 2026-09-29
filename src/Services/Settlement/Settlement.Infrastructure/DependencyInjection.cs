using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Configuration;
using BuildingBlocks.Shared.Resilience;
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
            options.UseNpgsql(configuration.GetConnectionString("SettlementDb"))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<ISettlementBatchRepository, SettlementBatchRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILedgerService, GrpcLedgerService>();

        services.AddServiceTokenProvider(configuration, "Settlement");

        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        var ledgerGrpcAddress = configuration["Grpc:Ledger:Address"] ?? "http://ledger-api:5001";
        var ledgerGrpcAllowInsecure = configuration.GetValue<bool>("Grpc:Ledger:AllowInsecure");
        ServiceTokenExtensions.RequireGrpcTls(configuration, ledgerGrpcAddress, ledgerGrpcAllowInsecure);
        var ledgerGrpcInsecure = ServiceTokenExtensions.ShouldUseInsecureChannel(ledgerGrpcAddress);
        services.AddGrpcClient<LedgerService.LedgerServiceClient>(o =>
        {
            o.Address = new Uri(ledgerGrpcAddress);
            if (ledgerGrpcInsecure)
                o.ChannelOptionsActions.Add(channel =>
                    channel.UnsafeUseInsecureChannelCallCredentials = true);
        })
        .AddGrpcResilienceInterceptor()
        .AddServiceTokenAuthentication();

        return services;
    }
}