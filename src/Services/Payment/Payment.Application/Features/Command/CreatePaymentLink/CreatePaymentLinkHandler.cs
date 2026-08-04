using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.CreatePaymentLink;

public class CreatePaymentLinkHandler
{
    private readonly IPaymentLinkRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePaymentLinkCommand> _validator;
    private readonly ILogger<CreatePaymentLinkHandler> _logger;

    public CreatePaymentLinkHandler(
        IPaymentLinkRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreatePaymentLinkCommand> validator,
        ILogger<CreatePaymentLinkHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<CreatePaymentLinkResponse>> Handle(CreatePaymentLinkCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CreatePaymentLinkCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var link = new PaymentLink(
            Guid.NewGuid(),
            command.MerchantId,
            new Money(command.Amount, command.Currency),
            $"pl_{Guid.NewGuid().ToString("N")[..24]}",
            command.Description);

        await _repository.AddAsync(link, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created payment link {Code} for Merchant {MerchantId}", link.Code, command.MerchantId);

        return new CreatePaymentLinkResponse(link.Id, link.Amount.Amount, link.Amount.Currency, link.Code, link.Description, link.Active);
    }
}
