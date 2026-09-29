using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Queries.GetAllBalances;

public class GetAllBalancesHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly ILogger<GetAllBalancesHandler> _logger;

    public GetAllBalancesHandler(ILedgerAccountRepository repository, ILogger<GetAllBalancesHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<BalanceDto>>> Handle(GetAllBalancesQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(GetAllBalancesQuery), query.MerchantId);
        var accounts = await _repository.ListByMerchantIdAsync(query.MerchantId, cancellationToken);

        if (accounts.Count == 0)
            return new List<BalanceDto>();

        return accounts.Select(a => new BalanceDto(a.MerchantId, a.AvailableBalance, a.PendingBalance, a.ReservedBalance, a.Currency)).ToList();
    }
}
