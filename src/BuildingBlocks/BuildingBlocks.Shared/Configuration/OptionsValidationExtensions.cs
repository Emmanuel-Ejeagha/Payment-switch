using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Shared.Configuration;

public static class OptionsValidationExtensions
{
    public static IServiceCollection AddValidatedOptions<T>(this IServiceCollection services,
        IConfiguration configuration, string sectionName,
        Func<T, bool> validate, string validationMessage) where T : class
    {
        services.AddOptions<T>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(validate, validationMessage)
            .ValidateOnStart();

        return services;
    }
}
