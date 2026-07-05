using Merchant.Application.Features.Commands.ActivateMerchant;
using Merchant.Application.Features.Commands.OnboardMerchant;
using Merchant.Application.Features.Commands.SuspendMerchant;
using Merchant.Application.Features.Commands.UpdateMerchantConfig;
using Merchant.Application.Features.Queries.GetMerchantByEmail;
using Merchant.Application.Features.Queries.GetMerchantById;
using Merchant.Application.Features.Queries.ListMerchants;
using Microsoft.Extensions.DependencyInjection;

namespace Merchant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMerchantApplication(this IServiceCollection services)
    {
        services.AddScoped<OnboardMerchantHandler>();
        services.AddScoped<ActivateMerchantHandler>();
        services.AddScoped<SuspendMerchantHandler>();
        services.AddScoped<UpdateMerchantConfigurationHandler>();
        services.AddScoped<GetMerchantByIdHandler>();
        services.AddScoped<GetMerchantByEmailHandler>();
        services.AddScoped<ListMerchantsHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddValidatorsFromAssemblyContaining<OnboardMerchantCommandValidator>();

        return services;
    }
}