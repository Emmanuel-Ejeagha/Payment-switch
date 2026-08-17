using Merchant.Application.Features.Commands.ActivateMerchant;
using Merchant.Application.Features.Commands.ApproveMerchant;
using Merchant.Application.Features.Commands.GenerateMerchantApiKey;
using Merchant.Application.Features.Commands.OnboardMerchant;
using Merchant.Application.Features.Commands.ReactivateMerchant;
using Merchant.Application.Features.Commands.RejectMerchant;
using Merchant.Application.Features.Commands.RevokeMerchantApiKey;
using Merchant.Application.Features.Commands.RotateWebhookSecret;
using Merchant.Application.Features.Commands.SuspendMerchant;
using Merchant.Application.Features.Commands.UpdateContactDetails;
using Merchant.Application.Features.Commands.UpdateMerchantConfig;
using Merchant.Application.Features.Commands.UpdateSettlementInfo;
using Merchant.Application.Features.Queries.GetMerchantApiKeys;
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
        services.AddScoped<ApproveMerchantHandler>();
        services.AddScoped<RejectMerchantHandler>();
        services.AddScoped<ReactivateMerchantHandler>();
        services.AddScoped<SuspendMerchantHandler>();
        services.AddScoped<UpdateMerchantConfigurationHandler>();
        services.AddScoped<UpdateSettlementInfoHandler>();
        services.AddScoped<UpdateContactDetailsHandler>();
        services.AddScoped<GenerateMerchantApiKeyHandler>();
        services.AddScoped<RevokeMerchantApiKeyHandler>();
        services.AddScoped<RotateWebhookSecretHandler>();
        services.AddScoped<GetMerchantByIdHandler>();
        services.AddScoped<GetMerchantByEmailHandler>();
        services.AddScoped<ListMerchantsHandler>();
        services.AddScoped<GetMerchantApiKeysHandler>();

        services.AddValidatorsFromAssemblyContaining<OnboardMerchantCommandValidator>();

        return services;
    }
}