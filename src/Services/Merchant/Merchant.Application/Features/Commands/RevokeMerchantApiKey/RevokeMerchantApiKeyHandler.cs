using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.RevokeMerchantApiKey;

public class RevokeMerchantApiKeyHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RevokeMerchantApiKeyCommand> _validator;
    private readonly ILogger<RevokeMerchantApiKeyHandler> _logger;
    private readonly IApiKeyRevocationNotifier _revocationNotifier;

    public RevokeMerchantApiKeyHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<RevokeMerchantApiKeyCommand> validator,
        ILogger<RevokeMerchantApiKeyHandler> logger,
        IApiKeyRevocationNotifier revocationNotifier)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
        _revocationNotifier = revocationNotifier;
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

        // Best-effort: purge Payment's cached resolution so the revoked key
        // stops working immediately (Step 7.4). Never fails the command; the
        // short cache TTL backstops a missed notify.
        await _revocationNotifier.NotifyRevokedAsync(command.MerchantId, cancellationToken);
        return Result.Success();
    }
}
