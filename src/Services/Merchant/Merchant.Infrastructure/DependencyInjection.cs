using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Configuration;
using Merchant.Application.Interfaces;
using Merchant.Infrastructure.Messaging;
using Merchant.Infrastructure.Outbox;
using Merchant.Infrastructure.Persistence;
using Merchant.Infrastructure.Persistence.Repositories;
using Merchant.Infrastructure.Security;
using Merchant.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Merchant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMerchantInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var webhookSecretEncryptionKey = configuration["WebhookSecretEncryption:Key"]
            ?? throw new InvalidOperationException("WebhookSecretEncryption:Key is required (TASK-006).");
        WebhookSecretEncryptor.Initialize(webhookSecretEncryptionKey);

        services.AddScoped<OutboxInterceptor>();
        services.AddScoped<WebhookSecretEncryptionBackfill>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("MerchantDb"))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddServiceTokenProvider(configuration, "Merchant");
        services.AddOptions<PaymentPurgeOptions>()
            .Bind(configuration.GetSection(PaymentPurgeOptions.SectionName));
        services.AddScoped<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(5) });
        services.AddScoped<IApiKeyRevocationNotifier, PaymentApiKeyPurgeNotifier>();
        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}