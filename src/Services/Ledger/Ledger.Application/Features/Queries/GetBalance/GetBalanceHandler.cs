using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Queries.GetBalance;

public class GetBalanceHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly ILogger<GetBalanceHandler> _logger;

    public GetBalanceHandler(ILedgerAccountRepository repository, ILogger<GetBalanceHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<BalanceDto>> Handle(GetBalanceQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(GetBalanceQuery), query.MerchantId);
        var account = await _repository.GetByMerchantIdAsync(query.MerchantId, cancellationToken);
        if (account is null)
            return LedgerErrors.AccountNotFound(query.MerchantId);

        return new BalanceDto(account.MerchantId, account.AvailableBalance, account.PendingBalance, account.ReservedBalance, account.Currency);
    }
}