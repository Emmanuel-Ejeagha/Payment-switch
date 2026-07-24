using BuildingBlocks.Shared.Events;
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
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<RefundPaymentCommand> _validator;
    private readonly ILogger<RefundPaymentHandler> _logger;

    public RefundPaymentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<RefundPaymentCommand> validator,
        ILogger<RefundPaymentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
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

        Money? amount = command.Amount.HasValue ? new Money(command.Amount.Value, intent.Amount.Currency) : null;
        var refundAmount = amount ?? new Money(intent.Amount.Amount, intent.Amount.Currency);

        var gatewayResult = await _gateway.RefundAsync(intent.MerchantId, intent.GatewayReference!, refundAmount, cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.RefundFailed", gatewayResult.Errors.First().Message);

        intent.Refund(amount);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);

        var refundTx = intent.Transactions.Last(t => t.Type == TransactionType.Refund);
        return new RefundPaymentResponse(refundTx.Id, intent.Status.Value);
    }
}
