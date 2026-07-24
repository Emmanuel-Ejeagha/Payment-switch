using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Ledger.Application.Interfaces;
using Ledger.Domain.DomainErrors;
using Ledger.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Ledger.Application.Features.Commands.ReserveFunds;

public class ReserveFundsHandler
{
    private readonly ILedgerAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<ReserveFundsCommand> _validator;
    private readonly ILogger<ReserveFundsHandler> _logger;

    public ReserveFundsHandler(
        ILedgerAccountRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<ReserveFundsCommand> validator,
        ILogger<ReserveFundsHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ReserveFundsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(ReserveFundsCommand), command.MerchantId);
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
            account.ReserveFunds(amount, correlationId);
        }
        catch (InvalidOperationException ex)
        {
            return new Error("Ledger.ReserveFailed", ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(account.DomainEvents, cancellationToken);

        return Result.Success();
    }
}