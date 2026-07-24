using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Payment.Application.Interfaces;
using Payment.Domain;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Command.VoidPayment;

public class VoidPaymentHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly IPaymentGatewayService _gateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<VoidPaymentCommand> _validator;
    private readonly ILogger<VoidPaymentHandler> _logger;

    public VoidPaymentHandler(
        IPaymentIntentRepository repository,
        IPaymentGatewayService gateway,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<VoidPaymentCommand> validator,
        ILogger<VoidPaymentHandler> logger)
    {
        _repository = repository;
        _gateway = gateway;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
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

        var gatewayResult = await _gateway.VoidAsync(intent.MerchantId, intent.GatewayReference!, cancellationToken);
        if (!gatewayResult.IsSuccess)
            return new Error("Payment.VoidFailed", gatewayResult.Errors.First().Message);

        intent.Void();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(intent.DomainEvents, cancellationToken);

        return new VoidPaymentResponse(intent.Status.Value);
    }
}
