using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Configuration;
using BuildingBlocks.Shared.Resilience;
using BuildingBlocks.Shared.Retention;
using Notification.Application.Interfaces;
using Notification.Infrastructure.DeadLetter;
using Notification.Infrastructure.Outbox;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Senders;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Retention;
using Notification.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentSwitch.Protos.Merchant;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OutboxInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb"))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMerchantContactService, GrpcMerchantContactService>();

        services.AddScoped<EmailSender>();
        services.AddScoped<SmsSender>();
        services.AddScoped<WebhookSender>();
        services.AddScoped<INotificationSender, NotificationSenderDispatcher>();

        services.AddServiceTokenProvider(configuration, "Notification");

        services.AddHostedService<NotificationSenderBackgroundService>();
        services.AddHostedService<RabbitMQConsumerService>();
        services.AddHostedService<OutboxPublisherService>();
        services.AddHostedService<NotificationRetentionService>();
        services.AddHostedService<DeadLetterConsumerService>();

        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddOptions<NotificationRetentionOptions>()
            .Bind(configuration.GetSection(NotificationRetentionOptions.SectionName));
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        services.AddScoped<HttpClient>(_ => new HttpClient());

        // The merchant gRPC channel defaults to the isolated docker/k8s network
        // (port 5001 is never published; endpoint is ServiceOnly-gated).
        // Production requires https unless Grpc:Merchant:AllowInsecure=true
        // (see docs/tls.md "gRPC service-to-service"). Payment events are
        // enriched with the merchant contact email at consume time so
        // notifications never use a placeholder.
        var merchantGrpcAddress = configuration["Grpc:Merchant:Address"] ?? "http://merchant-api:5001";
        var merchantGrpcAllowInsecure = configuration.GetValue<bool>("Grpc:Merchant:AllowInsecure");
        ServiceTokenExtensions.RequireGrpcTls(configuration, merchantGrpcAddress, merchantGrpcAllowInsecure);
        var merchantGrpcInsecure = ServiceTokenExtensions.ShouldUseInsecureChannel(merchantGrpcAddress);
        services.AddGrpcClient<MerchantService.MerchantServiceClient>(o =>
        {
            o.Address = new Uri(merchantGrpcAddress);
            if (merchantGrpcInsecure)
                o.ChannelOptionsActions.Add(channel =>
                    channel.UnsafeUseInsecureChannelCallCredentials = true);
        })
        .AddGrpcResilienceInterceptor()
        .AddServiceTokenAuthentication();

        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.Configure<SmsSettings>(configuration.GetSection("Sms"));

        return services;
    }
}