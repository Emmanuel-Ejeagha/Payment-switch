using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Ledger.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Commands.RefundFunds;

public class RefundFundsHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<RefundFundsCommand> _validator;
    private readonly ILogger<RefundFundsHandler> _logger;

    public RefundFundsHandler(
        ILedgerAccountRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<RefundFundsCommand> validator,
        ILogger<RefundFundsHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(RefundFundsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(RefundFundsCommand), command.MerchantId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var account = await _repository.GetByMerchantIdAsync(command.MerchantId, cancellationToken);
        if (account is null)
            return LedgerErrors.AccountNotFound(command.MerchantId);

        try
        {
            var amount = new Money(command.Amount, command.Currency);
            var correlationId = new CorrelationId(command.CorrelationId);
            account.RefundFunds(amount, correlationId);
        }
        catch (InvalidOperationException ex)
        {
            return new Error("Ledger.RefundFailed", ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(account.DomainEvents, cancellationToken);

        return Result.Success();
    }
}