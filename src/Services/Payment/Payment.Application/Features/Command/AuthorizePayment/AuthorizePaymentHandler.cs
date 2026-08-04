using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Command.AuthorizePayment;

public class AuthorizePaymentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentGatewayService _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<AuthorizePaymentCommand> _validator;
    private readonly IMerchantService _merchantService;
    private readonly ILogger<AuthorizePaymentHandler> _logger;

    public AuthorizePaymentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<AuthorizePaymentCommand> validator,
        IMerchantService merchantService,
        ILogger<AuthorizePaymentHandler> logger)                
    {
        _repository = repository;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _merchantService = merchantService;
        _logger = logger;
    }

    public async Task<Result<AuthorizePaymentResponse>> Handle(AuthorizePaymentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Intent {IntentId}", nameof(AuthorizePaymentCommand), command.IntentId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var intent = await _repository.GetByIdAsync(command.IntentId, cancellationToken);
        if (intent is null) return PaymentErrors.PaymentIntentNotFound(command.IntentId);

        if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            var replay = intent.Transactions.FirstOrDefault(t => t.Type == TransactionType.Authorization && t.IdempotencyKey == command.IdempotencyKey);
            if (replay is not null)
            {
                _logger.LogInformation("Replaying authorize for Intent {IntentId} with key {Key}", intent.Id, command.IdempotencyKey);
                return new AuthorizePaymentResponse(intent.AuthorizationCode!.Value, intent.GatewayReference!.Value, intent.Status.Value);
            }
        }

        var statusResult = await _merchantService.GetMerchantStatusAsync(intent.MerchantId, cancellationToken);
        if (!statusResult.IsSuccess) return Result<AuthorizePaymentResponse>.Failure(statusResult.Errors);
        if (!string.Equals(statusResult.Value, "Active", StringComparison.OrdinalIgnoreCase)) return new Error("Payment.MerchantNotActive", "Merchant is not active.");

        if (intent.Status != PaymentStatus.Pending)
            return PaymentErrors.InvalidStatusTransition(intent.Status.Value, "Authorized");

        var gatewayResult = await _gateway.AuthorizeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.AuthorizationFailed", gatewayResult.Errors.First().Message);

        var gwResponse = gatewayResult.Value!;
        var gatewayRef = new GatewayReference(gwResponse.GatewayReference!);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (gwResponse.RequiresChallenge)
            {
                intent.RequireAction(gatewayRef);
            }
            else
            {
                var authCode = new AuthorizationCode(gwResponse.AuthorizationCode!);
                intent.Authorize(authCode, gatewayRef, command.IdempotencyKey);
            }
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

        return new AuthorizePaymentResponse(intent.AuthorizationCode?.Value ?? gwResponse.GatewayReference!, gwResponse.GatewayReference!, intent.Status.Value);
    }
}