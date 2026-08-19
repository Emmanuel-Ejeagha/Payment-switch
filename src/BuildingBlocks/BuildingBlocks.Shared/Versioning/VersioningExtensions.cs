using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Shared.Versioning;

public static class VersioningExtensions
{
    public static IServiceCollection AddPaymentSwitchVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Contract (docs/api-versioning.md): the version segment is mandatory in
            // the URL. A missing, unknown, unsupported, or malformed segment matches
            // no route and is rejected with 404; supported versions are reported on
            // 2xx responses via the `api-supported-versions` header.
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
