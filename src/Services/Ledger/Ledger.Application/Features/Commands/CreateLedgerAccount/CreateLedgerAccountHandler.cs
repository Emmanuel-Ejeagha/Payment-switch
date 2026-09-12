using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Ledger.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Commands.CreateLedgerAccount;

public class CreateLedgerAccountHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateLedgerAccountCommand> _validator;
    private readonly ILogger<CreateLedgerAccountHandler> _logger;

    public CreateLedgerAccountHandler(
        ILedgerAccountRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateLedgerAccountCommand> validator,
        ILogger<CreateLedgerAccountHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(CreateLedgerAccountCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CreateLedgerAccountCommand), command.MerchantId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var existing = await _repository.GetByMerchantIdAndCurrencyAsync(command.MerchantId, command.Currency, cancellationToken);
        if (existing is not null)
            return Result.Success();

        var account = new LedgerAccount(Guid.NewGuid(), command.MerchantId, command.Currency);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _repository.AddAsync(account, cancellationToken);
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
        catch (UniqueConstraintViolationException)
        {
            // A concurrent create won the insert race (unique
            // MerchantId+Currency). That is the desired end state, so converge
            // on success instead of failing.
            await _unitOfWork.RollbackAsync(cancellationToken);
            _logger.LogInformation("Ledger account for Merchant {MerchantId}/{Currency} already created concurrently", command.MerchantId, command.Currency);
            return Result.Success();
        }

        return Result.Success();
    }
}