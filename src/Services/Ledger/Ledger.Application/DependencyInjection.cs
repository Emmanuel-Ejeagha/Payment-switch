using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Events;
using FluentValidation;
using Ledger.Application.Features.Commands.CaptureFunds;
using Ledger.Application.Features.Commands.CreateLedgerAccount;
using Ledger.Application.Features.Commands.RefundFunds;
using Ledger.Application.Features.Commands.ReserveFunds;
using Ledger.Application.Features.Queries.GetBalance;
using Ledger.Application.Features.Queries.GetTransactionHistory;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLedgerApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateLedgerAccountHandler>();
        services.AddScoped<ReserveFundsHandler>();
        services.AddScoped<CaptureFundsHandler>();
        services.AddScoped<RefundFundsHandler>();
        services.AddScoped<GetBalanceHandler>();
        services.AddScoped<GetTransactionHistoryHandler>();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddValidatorsFromAssemblyContaining<CreateLedgerAccountCommandValidator>();

        return services;
    }
}