using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;
using Settlement.Domain.DomainErrors;
using Settlement.Domain.Entities;
using Settlement.Domain.ValueObjects;

namespace Settlement.Application.Features.Command.TriggerSettlement;

public class TriggerSettlementHandler
{
    private readonly ISettlementBatchRepository _repository;
    private readonly ILedgerService _ledgerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<TriggerSettlementCommand> _validator;
    private readonly ILogger<TriggerSettlementHandler> _logger;

    public TriggerSettlementHandler(
        ISettlementBatchRepository repository,
        ILedgerService ledgerService,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<TriggerSettlementCommand> validator,
        ILogger<TriggerSettlementHandler> logger)
    {
        _repository = repository;
        _ledgerService = ledgerService;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<TriggerSettlementResponse>> Handle(TriggerSettlementCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName}", nameof(TriggerSettlementCommand));

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var batchDate = command.BatchDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(command.BatchDate, DateTimeKind.Utc)
            : command.BatchDate;

        var existing = await _repository.GetByBatchDateAsync(batchDate, cancellationToken);
        if (existing is not null)
            return new TriggerSettlementResponse(existing.Id);

        var ledgerResult = await _ledgerService.GetDailyPayoutDataAsync(batchDate, cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result<TriggerSettlementResponse>.Failure(ledgerResult.Errors);

        var payoutDataList = ledgerResult.Value!;

        var batch = new SettlementBatch(Guid.NewGuid(), batchDate);

        foreach (var data in payoutDataList)
        {
            var gross = new Money(data.GrossVolume, data.Currency);
            var fees = new Money(data.Fees, data.Currency);
            batch.AddPayout(data.MerchantId, gross, fees);
        }

        batch.Complete();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _repository.AddAsync(batch, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return SettlementErrors.ConcurrencyConflict;
        }
        await _dispatcher.DispatchAsync(batch.DomainEvents, cancellationToken);

        return new TriggerSettlementResponse(batch.Id);
    }
}