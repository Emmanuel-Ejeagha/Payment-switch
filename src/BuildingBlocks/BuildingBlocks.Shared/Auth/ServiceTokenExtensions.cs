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

        var secret = section["Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "ServiceToken:Secret is not configured. Set a dedicated service secret (ServiceToken__Secret); "
                + "falling back to Jwt:Secret would let a user-secret compromise forge inter-service calls.");
        }

        if (string.Equals(secret, jwt["Secret"], StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "ServiceToken:Secret must differ from Jwt:Secret so a user-secret compromise cannot forge inter-service calls.");
        }

        var options = new ServiceTokenOptions
        {
            ServiceName = section["ServiceName"] ?? serviceName,
            ExpiryMinutes = int.TryParse(section["ExpiryMinutes"], out var minutes) ? minutes : 10,
            Issuer = section["Issuer"] ?? jwt["Issuer"] ?? "IdentityService",
            Audience = section["Audience"] ?? jwt["Audience"] ?? "PaymentSwitch",
            Secret = secret
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
