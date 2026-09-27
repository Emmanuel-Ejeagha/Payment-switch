using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Services;

namespace Payment.Application.Features.Command.CreateCardToken;

public class CreateCardTokenHandler
{
    private readonly ICardTokenRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCardTokenCommand> _validator;
    private readonly ILogger<CreateCardTokenHandler> _logger;

    public CreateCardTokenHandler(
        ICardTokenRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCardTokenCommand> validator,
        ILogger<CreateCardTokenHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CreateCardTokenResponse>> Handle(CreateCardTokenCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CreateCardTokenCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        if (!CardValidation.IsValidExpiry(command.ExpiryMonth, command.ExpiryYear))
            return new Error("Card.InvalidExpiry", "Card has expired.");

        var token = new CardToken(
            command.MerchantId,
            $"card_{Guid.NewGuid().ToString("N")}",
            command.CardNumber[^4..],
            CardValidation.DetectBrand(command.CardNumber),
            command.ExpiryMonth,
            command.ExpiryYear);

        await _repository.AddAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tokenized card for Merchant {MerchantId} (last4 {LastFour})", command.MerchantId, token.LastFour);

        return new CreateCardTokenResponse(token.Token, token.LastFour, token.Brand, token.ExpiryMonth, token.ExpiryYear);
    }
}
