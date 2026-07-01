using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Ledger.Domain.ValueObjects;

namespace Ledger.Application.Features.Commands.CaptureFunds;

public class CaptureFundsHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<CaptureFundsCommand> _validator;

    public CaptureFundsHandler(
        ILedgerAccountRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<CaptureFundsCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result> Handle(CaptureFundsCommand command, CancellationToken cancellationToken = default)
    {
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
            account.CaptureFunds(amount, correlationId);
        }
        catch (InvalidOperationException ex)
        {
            return new Error("Ledger.CaptureFailed", ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(account.DomainEvents, cancellationToken);

        return Result.Success();
    }
}