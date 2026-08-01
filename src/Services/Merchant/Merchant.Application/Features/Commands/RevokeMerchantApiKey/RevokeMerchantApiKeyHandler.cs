using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.RevokeMerchantApiKey;

public class RevokeMerchantApiKeyHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RevokeMerchantApiKeyCommand> _validator;
    private readonly ILogger<RevokeMerchantApiKeyHandler> _logger;

    public RevokeMerchantApiKeyHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<RevokeMerchantApiKeyCommand> validator,
        ILogger<RevokeMerchantApiKeyHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeMerchantApiKeyCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId} Key {KeyId}", nameof(RevokeMerchantApiKeyCommand), command.MerchantId, command.KeyId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdWithApiKeysAsync(command.MerchantId, cancellationToken);
        if (merchant is null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        if (!command.Caller.CanAccess(merchant.OwnerId))
            return MerchantErrors.Unauthorized();

        try
        {
            merchant.RevokeApiKey(command.KeyId);
        }
        catch (InvalidOperationException)
        {
            return new Error("Merchant.ApiKeyNotFound", $"API key with Id '{command.KeyId}' not found.");
        }

        await _repository.UpdateAsync(merchant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
