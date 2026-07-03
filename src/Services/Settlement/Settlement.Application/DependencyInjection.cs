using BuildingBlocks.Shared.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Settlement.Application.Features.Command.TriggerSettlement;
using Settlement.Application.Features.Queries.GetSettlementBatch;
using Settlement.Application.Features.Queries.ListSettlementBatches;

namespace Settlement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSettlementApplication(this IServiceCollection services)
    {
        services.AddScoped<TriggerSettlementHandler>();
        services.AddScoped<GetSettlementBatchHandler>();
        services.AddScoped<ListSettlementBatchesHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddValidatorsFromAssemblyContaining<TriggerSettlementCommandValidator>();

        return services;
    }
}