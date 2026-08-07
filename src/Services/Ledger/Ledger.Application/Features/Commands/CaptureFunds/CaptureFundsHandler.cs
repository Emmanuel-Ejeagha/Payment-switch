using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Ledger.Application.Common;
using Ledger.Application.Interfaces;
using Ledger.Application.Options;
using Ledger.Domain.DomainErrors;
using Ledger.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ledger.Application.Features.Commands.CaptureFunds;

public class CaptureFundsHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<CaptureFundsCommand> _validator;
    private readonly LedgerOptions _options;
    private readonly ILogger<CaptureFundsHandler> _logger;

    public CaptureFundsHandler(
        ILedgerAccountRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<CaptureFundsCommand> validator,
        IOptions<LedgerOptions> options,
        ILogger<CaptureFundsHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result> Handle(CaptureFundsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CaptureFundsCommand), command.MerchantId);
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
            account.CaptureFunds(amount, new CorrelationId(command.CorrelationId));

            var fee = FeeCalculator.Calculate(command.Amount, _options.FeeBasisPoints);
            if (fee > 0)
            {
                _logger.LogInformation("Charging {Fee} processing fee for Merchant {MerchantId}", fee, command.MerchantId);
                account.ChargeFees(new Money(fee, command.Currency), new CorrelationId(command.CorrelationId));
            }
        }
        catch (InvalidOperationException ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return new Error("Ledger.CaptureFailed", ex.Message);
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

        await _dispatcher.DispatchAsync(account.DomainEvents, cancellationToken);

        return Result.Success();
    }
}