using BuildingBlocks.Shared;
using FluentValidation;
using Ledger.Application.Features.Commands.CaptureFunds;
using Ledger.Application.Features.Commands.CreateLedgerAccount;
using Ledger.Application.Features.Commands.RefundFunds;
using Ledger.Application.Features.Commands.ReserveFunds;
using Ledger.Application.Features.Commands.RunReconciliation;
using Ledger.Application.Features.Queries.GetAllBalances;
using Ledger.Application.Features.Queries.GetBalance;
using Ledger.Application.Features.Queries.GetLatestReconciliation;
using Ledger.Application.Features.Queries.GetTransactionHistory;
using Ledger.Application.Features.Queries.ListReconciliationReports;
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
        services.AddScoped<GetAllBalancesHandler>();
        services.AddScoped<GetTransactionHistoryHandler>();
        services.AddScoped<RunReconciliationHandler>();
        services.AddScoped<GetLatestReconciliationHandler>();
        services.AddScoped<ListReconciliationReportsHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateLedgerAccountCommandValidator>();

        return services;
    }
}