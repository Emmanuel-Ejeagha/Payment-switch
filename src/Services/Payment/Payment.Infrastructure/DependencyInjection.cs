using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Outbox;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;
using Payment.Infrastructure.Services;
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
            options.UseNpgsql(configuration.GetConnectionString("PaymentDb"))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IPaymentIntentRepository, PaymentIntentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMerchantService, GrpcMerchantService>();

        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
        services.AddScoped<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        services.AddGrpcClient<MerchantService.MerchantServiceClient>(o =>
        {
            o.Address = new Uri("http://merchant-api:5001");
        });

        return services;
    }
}