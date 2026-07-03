using Notification.Application.Interfaces;
using Notification.Infrastructure.Outbox;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Senders;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<EmailSender>();
        services.AddScoped<SmsSender>();
        services.AddScoped<WebhookSender>();
        services.AddScoped<INotificationSender, NotificationSenderDispatcher>();

        services.AddHostedService<NotificationSenderBackgroundService>();
        services.AddHostedService<RabbitMQConsumerService>();
        services.AddHostedService<OutboxPublisherService>();

        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
        services.AddScoped<IEventBus, RabbitMQEventBus>();

        return services;
    }
}