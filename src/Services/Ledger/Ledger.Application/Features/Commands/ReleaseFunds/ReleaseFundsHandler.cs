using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Ledger.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Commands.ReleaseFunds;

public class ReleaseFundsHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ReleaseFundsCommand> _validator;
    private readonly ILogger<ReleaseFundsHandler> _logger;

    public ReleaseFundsHandler(
        ILedgerAccountRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<ReleaseFundsCommand> validator,
        ILogger<ReleaseFundsHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ReleaseFundsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(ReleaseFundsCommand), command.MerchantId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var account = await _repository.GetByMerchantIdAndCurrencyAsync(command.MerchantId, command.Currency, cancellationToken);
        if (account is null)
            return LedgerErrors.AccountNotFound(command.MerchantId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var amount = new Money(command.Amount, command.Currency);
            var correlationId = new CorrelationId(command.CorrelationId);
            account.ReleaseFunds(amount, correlationId);
        }
        catch (InvalidOperationException ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return new Error("Ledger.ReleaseFailed", ex.Message);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return LedgerErrors.ConcurrencyConflict;
        }

        return Result.Success();
    }
}
