using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Command.CapturePayment;

public class CapturePaymentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentGatewayService _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<CapturePaymentCommand> _validator;
    private readonly ILogger<CapturePaymentHandler> _logger;

    public CapturePaymentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<CapturePaymentCommand> validator,
        ILogger<CapturePaymentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CapturePaymentResponse>> Handle(CapturePaymentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Intent {IntentId}", nameof(CapturePaymentCommand), command.IntentId);
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var intent = await _repository.GetByIdAsync(command.IntentId, cancellationToken);
        if (intent is null)
            return PaymentErrors.PaymentIntentNotFound(command.IntentId);

        Money? amount = command.Amount.HasValue ? new Money(command.Amount.Value, intent.Amount.Currency) : null;

        var gatewayResult = await _gateway.CaptureAsync(intent.MerchantId, intent.GatewayReference!, amount ?? intent.Amount, cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.CaptureFailed", gatewayResult.Errors.First().Message);

        intent.Capture(amount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);

        var captureTx = intent.Transactions.Last(t => t.Type == TransactionType.Capture);
        return new CapturePaymentResponse(captureTx.Id, intent.Status.Value);
    }
}
