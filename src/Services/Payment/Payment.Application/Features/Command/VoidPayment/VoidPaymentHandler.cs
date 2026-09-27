using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Command.VoidPayment;

public class VoidPaymentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentGatewayService _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<VoidPaymentCommand> _validator;
    private readonly ILogger<VoidPaymentHandler> _logger;

    public VoidPaymentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IUnitOfWork unitOfWork,
        IValidator<VoidPaymentCommand> validator,
        ILogger<VoidPaymentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VoidPaymentResponse>> Handle(VoidPaymentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Intent {IntentId}", nameof(VoidPaymentCommand), command.IntentId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var intent = await _repository.GetByIdAsync(command.IntentId, cancellationToken);
        if (intent is null)
            return PaymentErrors.PaymentIntentNotFound(command.IntentId);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var replay = intent.Transactions.FirstOrDefault(t => t.Type == TransactionType.Void && t.IdempotencyKey == command.IdempotencyKey);
            if (replay is not null)
            {
                _logger.LogInformation("Replaying void for Intent {IntentId} with key {Key}", intent.Id, command.IdempotencyKey);
                return new VoidPaymentResponse(intent.Status.Value);
            }
        }

        if (intent.Status != PaymentStatus.Authorized)
            return PaymentErrors.InvalidStatusTransition(intent.Status.Value, "Voided");

        var gatewayResult = await _gateway.VoidAsync(intent.MerchantId, intent.GatewayReference!, command.IdempotencyKey, intent.ProviderName, cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.VoidFailed", gatewayResult.Errors.First().Message);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            intent.Void(command.IdempotencyKey);
        }
        catch (InvalidOperationException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return PaymentErrors.InvalidStatusTransition(intent.Status.Value, "Voided");
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

        return new VoidPaymentResponse(intent.Status.Value);
    }
}
