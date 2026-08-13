using BuildingBlocks.Shared.Configuration;
using Ledger.Application.Interfaces;
using Ledger.Application.Options;
using Ledger.Infrastructure.DeadLetter;
using Ledger.Infrastructure.Inbox;
using Ledger.Infrastructure.Messaging;
using Ledger.Infrastructure.Outbox;
using Ledger.Infrastructure.Persistence;
using Ledger.Infrastructure.Persistence.Repositories;
using Ledger.Infrastructure.Queries;
using Ledger.Infrastructure.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLedgerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("LedgerDb"))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<ILedgerAccountRepository, LedgerAccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDailyPayoutQuery, DailyPayoutQuery>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IReconciliationReportRepository, ReconciliationReportRepository>();

        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddValidatedOptions<LedgerOptions>(configuration, "Ledger",
            o => o.FeeBasisPoints >= 0,
            "Ledger FeeBasisPoints must be >= 0");
        services.AddValidatedOptions<ReconciliationOptions>(configuration, "Reconciliation",
            o => o.IntervalMinutes >= 1,
            "Reconciliation IntervalMinutes must be >= 1");
        services.AddScoped<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();
        services.AddHostedService<RabbitMQConsumerService>();
        services.AddHostedService<InboxCleanupService>();
        services.AddHostedService<ReconciliationBackgroundService>();
        services.AddHostedService<DeadLetterConsumerService>();

        return services;
    }
}