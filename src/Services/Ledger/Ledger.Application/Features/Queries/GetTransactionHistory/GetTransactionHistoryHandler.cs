using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Queries.GetTransactionHistory;

public class GetTransactionHistoryHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly ILogger<GetTransactionHistoryHandler> _logger;

    public GetTransactionHistoryHandler(ILedgerAccountRepository repository, ILogger<GetTransactionHistoryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<TransactionDto>>> Handle(GetTransactionHistoryQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(GetTransactionHistoryQuery), query.MerchantId);
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
