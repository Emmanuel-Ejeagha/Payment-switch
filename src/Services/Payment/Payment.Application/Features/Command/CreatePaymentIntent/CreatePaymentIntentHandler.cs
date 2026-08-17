using BuildingBlocks.Shared.Exceptions;
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
    private readonly IMerchantService _merchantService;
    private readonly ICardTokenRepository _cardTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePaymentIntentCommand> _validator;
    private readonly ILogger<CreatePaymentIntentHandler> _logger;

    public CreatePaymentIntentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IMerchantService merchantService,
        ICardTokenRepository cardTokenRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreatePaymentIntentCommand> validator,
        ILogger<CreatePaymentIntentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _merchantService = merchantService;
        _cardTokenRepository = cardTokenRepository;
        _unitOfWork = unitOfWork;
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
        {
            _logger.LogInformation("Replaying create for Merchant {MerchantId} with key {Key}", command.MerchantId, command.IdempotencyKey);
            return ToResponse(existing);
        }

        var amount = new Money(command.Amount, command.Currency);
        var paymentMethod = ResolvePaymentMethod(command.PaymentMethod);
        var idempotencyKey = new IdempotencyKey(command.IdempotencyKey);
        CardDetails? cardDetails = null;
        if (paymentMethod == PaymentMethod.Card)
        {
            if (!string.IsNullOrWhiteSpace(command.CardToken))
            {
                var token = await _cardTokenRepository.GetByTokenAsync(command.MerchantId, command.CardToken, cancellationToken);
                if (token is null)
                    return new Error("Payment.InvalidCardToken", "The provided card token is invalid or does not belong to this merchant.");
                cardDetails = new CardDetails(token.LastFour, token.Brand, token.Token);
            }
            else
            {
                cardDetails = new CardDetails(command.CardLastFour!, command.CardBrand!);
            }
        }

        var intent = new PaymentIntent(Guid.NewGuid(), command.MerchantId, amount, idempotencyKey, paymentMethod, cardDetails);

        // Deliberately a local: the CVC goes to the gateway and dies with this scope.
        // It is never assigned to `intent`, which is what gets persisted.
        var securityCode = CardSecurityCode.IsValid(command.SecurityCode)
            ? new CardSecurityCode(command.SecurityCode!)
            : null;

        var configResult = await _merchantService.GetMerchantConfigAsync(command.MerchantId, cancellationToken);
        if (!configResult.IsSuccess)
            return new Error("Payment.MerchantConfigRetrievalFailed", "Unable to retrieve merchant configuration.");

        var authResult = await _gateway.AuthorizeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, securityCode, cancellationToken);
        if (!authResult.IsSuccess)
        {
            intent.Fail();
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await _repository.AddAsync(intent, cancellationToken);
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
            return new PaymentIntentResponse(intent.Id, intent.Status.Value, null);
        }

        var gwResponse = authResult.Value!;
        var gatewayRef = new GatewayReference(gwResponse.GatewayReference!);

        if (gwResponse.RequiresChallenge)
        {
            intent.RequireAction(gatewayRef);
        }
        else
        {
            var authCode = new AuthorizationCode(gwResponse.AuthorizationCode!);
            intent.Authorize(authCode, gatewayRef);

            if (configResult.Value!.AutoCapture)
                intent.Capture();
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _repository.AddAsync(intent, cancellationToken);
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

        return ToResponse(intent);
    }

    private static PaymentIntentResponse ToResponse(PaymentIntent intent)
    {
        string? clientSecret = intent.Transactions.LastOrDefault(t => t.Type == TransactionType.Capture)?.Id.ToString();
        return new PaymentIntentResponse(intent.Id, intent.Status.Value, clientSecret);
    }

    private static PaymentMethod ResolvePaymentMethod(string method) => method switch
    {
        "Card" => PaymentMethod.Card,
        "Bank" => PaymentMethod.Bank,
        "MobileMoney" => PaymentMethod.MobileMoney,
        _ => throw new ArgumentException($"Unknown payment method: {method}")
    };
}