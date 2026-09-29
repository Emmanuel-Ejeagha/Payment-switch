using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.RotateWebhookSecret;

public class RotateWebhookSecretHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RotateWebhookSecretCommand> _validator;
    private readonly ILogger<RotateWebhookSecretHandler> _logger;

    public RotateWebhookSecretHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<RotateWebhookSecretCommand> validator,
        ILogger<RotateWebhookSecretHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(RotateWebhookSecretCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(RotateWebhookSecretCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        if (!command.Caller.CanAccess(merchant.OwnerId))
            return MerchantErrors.Unauthorized();

        if (!command.Caller.EmailVerified)
            return MerchantErrors.EmailNotVerified();

        if (merchant.Status != MerchantStatus.Active)
            return MerchantErrors.MerchantNotActive();

        try
        {
            var secret = merchant.RotateWebhookSecret();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<string>.Success(secret.Value);
        }
        catch (InvalidOperationException)
        {
            return new Error("Merchant.ConfigurationUpdateFailed", "Cannot rotate webhook secret for this merchant.");
        }
    }
}
