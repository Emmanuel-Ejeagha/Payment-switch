using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Services;

namespace Payment.Application.Features.Command.CheckoutTokenize;

public class CheckoutTokenizeHandler
{
    private readonly IPaymentLinkRepository _linkRepository;
    private readonly ICardTokenRepository _cardTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CheckoutTokenizeCommand> _validator;
    private readonly ILogger<CheckoutTokenizeHandler> _logger;

    public CheckoutTokenizeHandler(
        IPaymentLinkRepository linkRepository,
        ICardTokenRepository cardTokenRepository,
        IUnitOfWork unitOfWork,
        IValidator<CheckoutTokenizeCommand> validator,
        ILogger<CheckoutTokenizeHandler> logger)
    {
        _linkRepository = linkRepository;
        _cardTokenRepository = cardTokenRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CheckoutTokenizeResponse>> Handle(CheckoutTokenizeCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for link {Code}", nameof(CheckoutTokenizeCommand), command.Code);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        if (!CardValidation.IsValidExpiry(command.ExpiryMonth, command.ExpiryYear))
            return new Error("Card.InvalidExpiry", "Card has expired.");

        var link = await _linkRepository.GetByCodeAsync(command.Code, cancellationToken);
        if (link is null)
            return new Error("PaymentLink.NotFound", "Payment link not found.");
        if (!link.Active)
            return new Error("PaymentLink.Inactive", "This payment link has been deactivated.");

        var token = new CardToken(
            link.MerchantId,
            $"card_{Guid.NewGuid().ToString("N")}",
            command.CardNumber[^4..],
            CardValidation.DetectBrand(command.CardNumber),
            command.ExpiryMonth,
            command.ExpiryYear);

        await _cardTokenRepository.AddAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CheckoutTokenizeResponse(token.Token, token.LastFour, token.Brand);
    }
}
