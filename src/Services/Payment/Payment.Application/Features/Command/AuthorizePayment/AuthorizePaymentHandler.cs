using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.Interfaces;
using Payment.Domain;
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

        var statusResult = await _merchantService.GetMerchantStatusAsync(intent.MerchantId, cancellationToken);
        if (!statusResult.IsSuccess) return Result<AuthorizePaymentResponse>.Failure(statusResult.Errors);
        if (statusResult.Value != "active") return new Error("Payment.MerchantNotActive", "Merchant is not active.");


        var gatewayResult = await _gateway.AuthorizeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.AuthorizationFailed", gatewayResult.Errors.First().Message);

        var gwResponse = gatewayResult.Value!;
        var authCode = new AuthorizationCode(gwResponse.AuthorizationCode!);
        var gatewayRef = new GatewayReference(gwResponse.GatewayReference!);

        intent.Authorize(authCode, gatewayRef);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);

        return new AuthorizePaymentResponse(authCode.Value, gatewayRef.Value, intent.Status.Value);
    }
}