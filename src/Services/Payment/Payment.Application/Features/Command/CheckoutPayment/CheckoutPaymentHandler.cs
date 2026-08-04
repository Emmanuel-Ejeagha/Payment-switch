using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Interfaces;

namespace Payment.Application.Features.Command.CheckoutPayment;

public class CheckoutPaymentHandler
{
    private readonly IPaymentLinkRepository _linkRepository;
    private readonly CreatePaymentIntentHandler _createIntentHandler;
    private readonly IValidator<CheckoutPaymentCommand> _validator;
    private readonly ILogger<CheckoutPaymentHandler> _logger;

    public CheckoutPaymentHandler(
        IPaymentLinkRepository linkRepository,
        CreatePaymentIntentHandler createIntentHandler,
        IValidator<CheckoutPaymentCommand> validator,
        ILogger<CheckoutPaymentHandler> logger)
    {
        _linkRepository = linkRepository;
        _createIntentHandler = createIntentHandler;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CheckoutPaymentResponse>> Handle(CheckoutPaymentCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for link {Code}", nameof(CheckoutPaymentCommand), command.Code);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var link = await _linkRepository.GetByCodeAsync(command.Code, cancellationToken);
        if (link is null)
            return new Error("PaymentLink.NotFound", "Payment link not found.");
        if (!link.Active)
            return new Error("PaymentLink.Inactive", "This payment link has been deactivated.");

        var createCommand = new CreatePaymentIntentCommand(
            link.MerchantId,
            link.Amount.Amount,
            link.Amount.Currency,
            "Card",
            null,
            null,
            command.IdempotencyKey,
            command.CardToken);

        var result = await _createIntentHandler.Handle(createCommand, cancellationToken);
        if (!result.IsSuccess)
            return Result<CheckoutPaymentResponse>.Failure(result.Errors);

        return new CheckoutPaymentResponse(result.Value.IntentId, result.Value.Status, result.Value.ClientSecret);
    }
}
