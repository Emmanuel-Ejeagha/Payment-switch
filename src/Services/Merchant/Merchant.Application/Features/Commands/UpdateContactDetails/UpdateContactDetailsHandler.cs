using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.UpdateContactDetails;

public class UpdateContactDetailsHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateContactDetailsCommand> _validator;
    private readonly ILogger<UpdateContactDetailsHandler> _logger;

    public UpdateContactDetailsHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateContactDetailsCommand> validator,
        ILogger<UpdateContactDetailsHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateContactDetailsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(UpdateContactDetailsCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        if (!command.Caller.CanAccess(merchant.OwnerId))
            return MerchantErrors.Unauthorized();

        merchant.UpdateContactDetails(new ContactDetails(command.Phone, command.Address, command.ContactPerson));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}