using BuildingBlocks.Shared.Configuration;
using BuildingBlocks.Shared.Email;
using Identity.Application.Configuration;
using Identity.Application.Interfaces;
using Identity.Infrastructure.Messaging;
using Identity.Infrastructure.Outbox;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<OutboxInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<OutboxInterceptor>();
            options.UseNpgsql(configuration.GetConnectionString("IdentityDb"))
                   .AddInterceptors(interceptor);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddValidatedOptions<JwtSettings>(configuration, "Jwt",
            s => !string.IsNullOrEmpty(s.Secret) && !string.IsNullOrEmpty(s.Issuer),
            "JWT Secret and Issuer are required");

        services.AddScoped<IEmailVerificationTokenFactory, EmailVerificationTokenFactory>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.Configure<EmailVerificationOptions>(configuration.GetSection("EmailVerification"));

        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddScoped<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}