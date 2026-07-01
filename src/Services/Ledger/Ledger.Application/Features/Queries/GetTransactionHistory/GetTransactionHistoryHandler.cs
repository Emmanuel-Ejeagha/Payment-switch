using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;

namespace Ledger.Application.Features.Queries.GetTransactionHistory;

public class GetTransactionHistoryHandler
{
    private readonly ILedgerAccountRepository _repository;

    public GetTransactionHistoryHandler(ILedgerAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<TransactionDto>>> Handle(GetTransactionHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var account = await _repository.GetByMerchantIdAsync(query.MerchantId, cancellationToken);
        if (account is null)
            return LedgerErrors.AccountNotFound(query.MerchantId);

        var transactions = account.Journal
            .OrderByDescending(j => j.Timestamp)
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(j => new TransactionDto(j.Id, j.Type.ToString(), j.Amount.Amount, j.Amount.Currency, j.Description, j.Timestamp))
            .ToList();

        return transactions;
    }
}
