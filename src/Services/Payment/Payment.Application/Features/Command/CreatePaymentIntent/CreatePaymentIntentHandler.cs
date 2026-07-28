using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Command.CreatePaymentIntent;

public class CreatePaymentIntentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentGatewayService _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<CreatePaymentIntentCommand> _validator;
    private readonly ILogger<CreatePaymentIntentHandler> _logger;

    public CreatePaymentIntentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<CreatePaymentIntentCommand> validator,
        ILogger<CreatePaymentIntentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PaymentIntentResponse>> Handle(CreatePaymentIntentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CreatePaymentIntentCommand), command.MerchantId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var existing = await _repository.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, cancellationToken);
        if (existing is not null)
            return PaymentErrors.IdempotencyKeyViolation(command.IdempotencyKey);

        var amount = new Money(command.Amount, command.Currency);
        var paymentMethod = ResolvePaymentMethod(command.PaymentMethod);
        var idempotencyKey = new IdempotencyKey(command.IdempotencyKey);
        CardDetails? cardDetails = null;
        if (paymentMethod == PaymentMethod.Card)
            cardDetails = new CardDetails(command.CardLastFour!, command.CardBrand!);

        var intent = new PaymentIntent(Guid.NewGuid(), command.MerchantId, amount, idempotencyKey, paymentMethod, cardDetails);

        await _repository.AddAsync(intent, cancellationToken);

        // Auto-authorize via mock gateway
        var authResult = await _gateway.AuthorizeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, cancellationToken);
        if (!authResult.IsSuccess)
        {
            intent.Fail();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);
            return new PaymentIntentResponse(intent.Id, intent.Status.Value, null);
        }

        var gwResponse = authResult.Value!;
        var authCode = new AuthorizationCode(gwResponse.AuthorizationCode!);
        var gatewayRef = new GatewayReference(gwResponse.GatewayReference!);
        intent.Authorize(authCode, gatewayRef);

        // Auto-capture full amount
        intent.Capture();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);

        var captureTx = intent.Transactions.Last(t => t.Type == TransactionType.Capture);
        return new PaymentIntentResponse(intent.Id, intent.Status.Value, captureTx.Id.ToString());
    }

    private static PaymentMethod ResolvePaymentMethod(string method) => method switch
    {
        "Card" => PaymentMethod.Card,
        "Bank" => PaymentMethod.Bank,
        "MobileMoney" => PaymentMethod.MobileMoney,
        _ => throw new ArgumentException($"Unknown payment method: {method}")
    };
}