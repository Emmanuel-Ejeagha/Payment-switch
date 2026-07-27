using BuildingBlocks.Shared.Results;
using Ledger.Application.DTOs;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Ledger.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Queries.GetBalance;

public class GetBalanceHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetBalanceHandler> _logger;

    public GetBalanceHandler(ILedgerAccountRepository repository, IUnitOfWork unitOfWork, ILogger<GetBalanceHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BalanceDto>> Handle(GetBalanceQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(GetBalanceQuery), query.MerchantId);
        var account = await _repository.GetByMerchantIdAsync(query.MerchantId, cancellationToken);
        if (account is null)
        {
            _logger.LogInformation("No ledger account found for merchant {MerchantId}; auto-creating", query.MerchantId);
            account = new LedgerAccount(Guid.NewGuid(), query.MerchantId, "NGN");
            await _repository.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new BalanceDto(account.MerchantId, account.AvailableBalance, account.PendingBalance, account.ReservedBalance, account.Currency);
    }
}