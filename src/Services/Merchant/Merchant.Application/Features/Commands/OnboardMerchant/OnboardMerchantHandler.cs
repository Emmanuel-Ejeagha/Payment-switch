using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.OnboardMerchant;

public class OnboardMerchantHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<OnboardMerchantCommand> _validator;
    private readonly ILogger<OnboardMerchantHandler> _logger;

    public OnboardMerchantHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<OnboardMerchantCommand> validator,
        ILogger<OnboardMerchantHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<OnboardMerchantResponse>> Handle(OnboardMerchantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName}", nameof(OnboardMerchantCommand));

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        if (await _repository.ExistsByEmailAsync(command.Email, cancellationToken))
            return MerchantErrors.EmailAlreadyInUse(command.Email);

        var businessName = new BusinessName(command.BusinessName);
        var email = new MerchantEmail(command.Email);
        var merchant = new MerchantEntity(Guid.NewGuid(), command.OwnerId, businessName, email);

        await _repository.AddAsync(merchant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OnboardMerchantResponse(merchant.Id);
    }
}