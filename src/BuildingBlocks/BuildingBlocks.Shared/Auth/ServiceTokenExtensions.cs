using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BuildingBlocks.Shared.Auth;

public static class ServiceTokenExtensions
{
    public static IServiceCollection AddServiceTokenProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var section = configuration.GetSection(ServiceTokenOptions.SectionName);
        var jwt = configuration.GetSection("Jwt");

        var options = new ServiceTokenOptions
        {
            ServiceName = section["ServiceName"] ?? serviceName,
            ExpiryMinutes = int.TryParse(section["ExpiryMinutes"], out var minutes) ? minutes : 10,
            Issuer = section["Issuer"] ?? jwt["Issuer"] ?? "IdentityService",
            Audience = section["Audience"] ?? jwt["Audience"] ?? "PaymentSwitch",
            Secret = section["Secret"] ?? jwt["Secret"]
                ?? throw new InvalidOperationException(
                    "Service token secret is not configured. Set ServiceToken:Secret or Jwt:Secret.")
        };

        services.AddSingleton(new ServiceTokenProvider(options));
        return services;
    }

    public static IHttpClientBuilder AddServiceTokenAuthentication(this IHttpClientBuilder builder)
    {
        return builder.AddCallCredentials((context, metadata, serviceProvider) =>
        {
            var provider = serviceProvider.GetRequiredService<ServiceTokenProvider>();
            metadata.Add("Authorization", $"Bearer {provider.GetToken()}");
            return Task.CompletedTask;
        });
    }
}
