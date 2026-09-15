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
        var isProduction = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Production", StringComparison.OrdinalIgnoreCase);

        services.AddValidatedOptions<SmtpSettings>(configuration, "Smtp",
            s => !isProduction || !string.IsNullOrWhiteSpace(s.Host),
            "Smtp:Host must be configured in Production");
        services.AddValidatedOptions<EmailVerificationOptions>(configuration, "EmailVerification",
            s => {
                if (!isProduction) return true;
                if (string.IsNullOrWhiteSpace(s.FrontendBaseUrl)) return false;
                return Uri.TryCreate(s.FrontendBaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
            },
            "EmailVerification:FrontendBaseUrl must be an https URL in Production");
        services.AddValidatedOptions<PasswordResetOptions>(configuration, "PasswordReset",
            s => {
                if (!isProduction) return true;
                if (string.IsNullOrWhiteSpace(s.FrontendBaseUrl)) return false;
                return Uri.TryCreate(s.FrontendBaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
            },
            "PasswordReset:FrontendBaseUrl must be an https URL in Production");

        services.AddValidatedOptions<RabbitMQSettings>(configuration, "RabbitMQ",
            s => !string.IsNullOrEmpty(s.HostName),
            "RabbitMQ HostName is required");
        services.AddSingleton<IEventBus, RabbitMQEventBus>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}