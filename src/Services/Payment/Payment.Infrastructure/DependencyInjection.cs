using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Configuration;
using BuildingBlocks.Shared.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Outbox;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;
using Payment.Infrastructure.Services;
using Payment.Infrastructure.Services.Gateways;
using PaymentSwitch.Protos.Merchant;


namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("PaymentDb"), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
            })
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IPaymentGatewayService, ResilientPaymentGatewayService>();
        services.AddSingleton<IPaymentGatewayProvider, PaystackMockGatewayProvider>();
        services.AddSingleton<IPaymentGatewayProvider, StripeMockGatewayProvider>();
        services.AddSingleton<GatewayProviderRegistry>();
        services.AddSingleton<GatewayRouter>();
        services.AddScoped<IPaymentIntentRepository, PaymentIntentRepository>();
        services.AddScoped<ICardTokenRepository, CardTokenRepository>();
        services.AddScoped<IPaymentLinkRepository, PaymentLinkRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMerchantService, GrpcMerchantService>();
        services.AddScoped<WebhookDispatcher>();
        services.AddHttpClient("webhook")
            .ConfigureHttpClient(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(30);
            });

        services.AddServiceTokenProvider(configuration, "Payment");

        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddScoped<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();
        services.AddHostedService<WebhookDispatchWorker>();

        services.AddGrpcClient<MerchantService.MerchantServiceClient>(o =>
        {
            o.Address = new Uri(configuration["Grpc:Merchant:Address"] ?? "http://merchant-api:8080");
            o.ChannelOptionsActions.Add(channel =>
                channel.UnsafeUseInsecureChannelCallCredentials = true);
        })
        .AddGrpcResilienceInterceptor()
        .AddServiceTokenAuthentication();

        return services;
    }
}