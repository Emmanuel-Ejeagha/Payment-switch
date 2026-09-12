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
    private readonly IValidator<TriggerSettlementCommand> _validator;
    private readonly ILogger<TriggerSettlementHandler> _logger;

    public TriggerSettlementHandler(
        ISettlementBatchRepository repository,
        ILedgerService ledgerService,
        IUnitOfWork unitOfWork,
        IValidator<TriggerSettlementCommand> validator,
        ILogger<TriggerSettlementHandler> logger)
    {
        _repository = repository;
        _ledgerService = ledgerService;
        _unitOfWork = unitOfWork;
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

        var ledgerResult = await _ledgerService.GetDailyPayoutDataAsync(batchDate, cancellationToken);
        if (!ledgerResult.IsSuccess)
            return Result<TriggerSettlementResponse>.Failure(ledgerResult.Errors);

        // One batch per currency: a bare TotalAmount is only meaningful within
        // a single currency, so each currency group settles independently.
        // (Existing batches are detected per currency inside the loop, so a
        // re-trigger converges without duplicating.)
        var batchIds = new List<Guid>();
        foreach (var group in ledgerResult.Value!.GroupBy(d => d.Currency, StringComparer.OrdinalIgnoreCase))
        {
            var batchId = await TriggerCurrencyBatchAsync(batchDate, group.Key, group.ToList(), cancellationToken);
            if (!batchId.IsSuccess)
                return Result<TriggerSettlementResponse>.Failure(batchId.Errors);
            batchIds.Add(batchId.Value);
        }

        return new TriggerSettlementResponse(batchIds);
    }

    private async Task<Result<Guid>> TriggerCurrencyBatchAsync(
        DateTime batchDate,
        string currency,
        List<MerchantPayoutData> payoutDataList,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByBatchDateAndCurrencyAsync(batchDate, currency, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var batch = new SettlementBatch(Guid.NewGuid(), batchDate);

        foreach (var data in payoutDataList)
        {
            var gross = new Money(data.GrossVolume, data.Currency);
            var fees = new Money(data.Fees, data.Currency);
            batch.AddPayout(data.MerchantId, gross, fees);
        }

        // Tie-out, scoped to this currency: re-query the ledger and refuse to
        // complete a batch whose totals drift from the ledger's daily figures
        // (e.g. activity landing mid-batch).
        var tieOutResult = await _ledgerService.GetDailyPayoutDataAsync(batchDate, cancellationToken);
        if (!tieOutResult.IsSuccess)
            return Result<Guid>.Failure(tieOutResult.Errors);

        var tieOutList = tieOutResult.Value!.Where(d =>
            string.Equals(d.Currency, currency, StringComparison.OrdinalIgnoreCase)).ToList();
        var ledgerGross = tieOutList.Sum(d => d.GrossVolume);
        var ledgerFees = tieOutList.Sum(d => d.Fees);
        var batchGross = batch.Payouts.Sum(p => p.GrossVolume.Amount);
        var batchFees = batch.Payouts.Sum(p => p.Fees.Amount);

        if (ledgerGross != batchGross || ledgerFees != batchFees)
            return Result<Guid>.Failure(SettlementErrors.LedgerTieOutMismatch);

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
        catch (UniqueConstraintViolationException)
        {
            // A concurrent trigger already created this currency's batch
            // (unique BatchDate+Currency index). Surface the existing batch.
            await _unitOfWork.RollbackAsync(cancellationToken);
            var existingBatch = await _repository.GetByBatchDateAndCurrencyAsync(batchDate, currency, cancellationToken);
            if (existingBatch is not null)
                return existingBatch.Id;
            return new Error("Settlement.BatchCreateFailed", "Could not create settlement batch.");
        }

        return batch.Id;
    }
}