using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.ConfirmPaymentIntent;

public class ConfirmPaymentIntentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentGatewayService _gateway;
    private readonly IMerchantService _merchantService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<ConfirmPaymentIntentCommand> _validator;
    private readonly ILogger<ConfirmPaymentIntentHandler> _logger;

    public ConfirmPaymentIntentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IMerchantService merchantService,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<ConfirmPaymentIntentCommand> validator,
        ILogger<ConfirmPaymentIntentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _merchantService = merchantService;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ConfirmPaymentIntentResponse>> Handle(ConfirmPaymentIntentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Intent {IntentId}", nameof(ConfirmPaymentIntentCommand), command.IntentId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var intent = await _repository.GetByIdAsync(command.IntentId, cancellationToken);
        if (intent is null) return PaymentErrors.PaymentIntentNotFound(command.IntentId);

        if (intent.MerchantId != command.MerchantId)
            return PaymentErrors.PaymentIntentNotFound(command.IntentId);

        if (intent.Status != PaymentStatus.RequiresAction)
            return PaymentErrors.InvalidStatusTransition(intent.Status.Value, "RequiresAction");

        var configResult = await _merchantService.GetMerchantConfigAsync(intent.MerchantId, cancellationToken);
        if (!configResult.IsSuccess)
            return new Error("Payment.MerchantConfigRetrievalFailed", "Unable to retrieve merchant configuration.");

        // No CVC here: the code was collected on the original authorization and is not
        // retained, so a challenge confirm is always cardholder-not-present to us.
        var gatewayResult = await _gateway.ConfirmChallengeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, intent.GatewayReference!.Value, cancellationToken: cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.ChallengeConfirmationFailed", gatewayResult.Errors.First().Message);

        var gwResponse = gatewayResult.Value!;
        var authCode = new AuthorizationCode(gwResponse.AuthorizationCode!);
        var gatewayRef = new GatewayReference(gwResponse.GatewayReference!);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            intent.MarkProcessing();
            intent.ConfirmAction(authCode, gatewayRef, command.IdempotencyKey);

            if (configResult.Value.AutoCapture)
                intent.Capture();
        }
        catch (InvalidOperationException)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            return PaymentErrors.InvalidStatusTransition(intent.Status.Value, "Authorized");
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

        await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);

        string? clientSecret = intent.Transactions.LastOrDefault(t => t.Type == TransactionType.Capture)?.Id.ToString();
        return new ConfirmPaymentIntentResponse(intent.Id, intent.Status.Value, clientSecret);
    }
}
