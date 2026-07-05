using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;

namespace Ledger.Application.Features.Queries.GetBalance;

public class GetBalanceHandler
{
    private readonly ILedgerAccountRepository _repository;

    public GetBalanceHandler(ILedgerAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<BalanceDto>> Handle(GetBalanceQuery query, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByMerchantIdAsync(query.MerchantId, cancellationToken);
        if (account is null)
            return LedgerErrors.AccountNotFound(query.MerchantId);

        return new BalanceDto(account.MerchantId, account.AvailableBalance, account.PendingBalance, account.ReservedBalance, account.Currency);
    }
}