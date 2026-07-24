using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Command.CreatePaymentIntent;

public class CreatePaymentIntentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<CreatePaymentIntentCommand> _validator;
    private readonly ILogger<CreatePaymentIntentHandler> _logger;

    public CreatePaymentIntentHandler(
        IPaymentIntentRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<CreatePaymentIntentCommand> validator,
        ILogger<CreatePaymentIntentHandler> logger)
    {
        _repository = repository;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);

        return new PaymentIntentResponse(intent.Id, intent.Status.Value, "fake-client-secret-" + intent.Id);
    }

    private static PaymentMethod ResolvePaymentMethod(string method) => method switch
    {
        "Card" => PaymentMethod.Card,
        "Bank" => PaymentMethod.Bank,
        "MobileMoney" => PaymentMethod.MobileMoney,
        _ => throw new ArgumentException($"Unknown payment method: {method}")
    };
}