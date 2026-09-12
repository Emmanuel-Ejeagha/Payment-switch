using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Command.RefundPayment;

public class RefundPaymentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentGatewayService _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RefundPaymentCommand> _validator;
    private readonly ILogger<RefundPaymentHandler> _logger;

    public RefundPaymentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IUnitOfWork unitOfWork,
        IValidator<RefundPaymentCommand> validator,
        ILogger<RefundPaymentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<RefundPaymentResponse>> Handle(RefundPaymentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Intent {IntentId}", nameof(RefundPaymentCommand), command.IntentId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var intent = await _repository.GetByIdAsync(command.IntentId, cancellationToken);
        if (intent is null)
            return PaymentErrors.PaymentIntentNotFound(command.IntentId);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var replay = intent.Transactions.FirstOrDefault(t => t.Type == TransactionType.Refund && t.IdempotencyKey == command.IdempotencyKey);
            if (replay is not null)
            {
                // Null amount defers to the recorded result; a concrete
                // different amount is a conflicting reuse, not a replay.
                if (command.Amount.HasValue && command.Amount.Value != replay.Amount.Amount)
                {
                    _logger.LogWarning("Idempotency key {Key} reused with different amount for Intent {IntentId}", command.IdempotencyKey, intent.Id);
                    return PaymentErrors.IdempotencyKeyConflict(command.IdempotencyKey!);
                }

                _logger.LogInformation("Replaying refund for Intent {IntentId} with key {Key}", intent.Id, command.IdempotencyKey);
                return new RefundPaymentResponse(replay.Id, intent.Status.Value);
            }
        }

        if (intent.Status != PaymentStatus.Captured && intent.Status != PaymentStatus.PartiallyCaptured && intent.Status != PaymentStatus.PartiallyRefunded)
            return PaymentErrors.InvalidStatusTransition(intent.Status.Value, "Refunded");

        Money? amount = command.Amount.HasValue ? new Money(command.Amount.Value, intent.Amount.Currency) : null;

        // Default to captured-minus-refunded so a null-amount refund after a
        // partial capture does not over-request the authorized total.
        var refundAmount = amount ?? intent.GetRefundableAmount();

        var gatewayResult = await _gateway.RefundAsync(intent.MerchantId, intent.GatewayReference!, refundAmount, command.IdempotencyKey, intent.ProviderName, cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.RefundFailed", gatewayResult.Errors.First().Message);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            intent.Refund(amount, command.IdempotencyKey);
        }
        catch (InvalidOperationException ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            if (ex.Message.Contains("exceeds available refund amount", StringComparison.OrdinalIgnoreCase))
            {
                var captured = intent.Transactions.Where(t => t.Type == TransactionType.Capture).Sum(t => t.Amount.Amount);
                return PaymentErrors.RefundExceedsCaptured(refundAmount.Amount, captured);
            }
            return PaymentErrors.InvalidStatusTransition(intent.Status.Value, "Refunded");
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return PaymentErrors.ConcurrencyConflict;
        }

        var refundTx = intent.Transactions.Last(t => t.Type == TransactionType.Refund);
        return new RefundPaymentResponse(refundTx.Id, intent.Status.Value);
    }
}
