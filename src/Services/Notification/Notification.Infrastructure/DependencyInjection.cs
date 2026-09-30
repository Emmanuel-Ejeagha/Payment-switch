using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Configuration;
using BuildingBlocks.Shared.Resilience;
using BuildingBlocks.Shared.Retention;
using Notification.Application.Configuration;
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
using Resend;

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
        services.AddScoped<ResendEmailSender>();
        services.AddScoped<SmsSender>();
        services.AddScoped<WebhookSender>();
        services.AddScoped<INotificationSender, NotificationSenderDispatcher>();

        // Resend (verification mail only). The token stays in ResendSettings;
        // per-call timeouts are enforced in ResendEmailSender; the client
        // timeout mirrors the setting as a backstop.
        var resendTimeoutSeconds = Math.Max(1, configuration.GetValue<int>("Resend:TimeoutSeconds", 30));
        services
            .AddResend(o => o.ApiToken = configuration["Resend:ApiKey"] ?? string.Empty)
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(resendTimeoutSeconds));

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

        // The merchant gRPC channel is a documented in-network exception
        // (TASK-004): port 5001 is never published and the endpoint is gated by
        // the ServiceOnly policy. Payment events are enriched with the merchant
        // contact email at consume time so notifications never use a placeholder.
        services.AddGrpcClient<MerchantService.MerchantServiceClient>(o =>
        {
            o.Address = new Uri(configuration["Grpc:Merchant:Address"] ?? "http://merchant-api:5001");
            o.ChannelOptionsActions.Add(channel =>
                channel.UnsafeUseInsecureChannelCallCredentials = true);
        })
        .AddGrpcResilienceInterceptor()
        .AddServiceTokenAuthentication();

        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.Configure<SmsSettings>(configuration.GetSection("Sms"));

        var isProduction = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Production", StringComparison.OrdinalIgnoreCase);

        services.AddValidatedOptions<ResendSettings>(configuration, "Resend",
            s => !isProduction || !string.IsNullOrWhiteSpace(s.ApiKey),
            "Resend:ApiKey must be configured in Production");
        services.AddValidatedOptions<VerificationEmailOptions>(configuration, "EmailVerification",
            s =>
            {
                if (!isProduction) return true;
                if (string.IsNullOrWhiteSpace(s.FrontendBaseUrl)) return false;
                return Uri.TryCreate(s.FrontendBaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
            },
            "EmailVerification:FrontendBaseUrl must be an https URL in Production");

        return services;
    }
}