using BuildingBlocks.Shared.Security;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Merchant.Application.Features.Commands.GenerateMerchantApiKey;

public class GenerateMerchantApiKeyHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<GenerateMerchantApiKeyCommand> _validator;
    private readonly ILogger<GenerateMerchantApiKeyHandler> _logger;

    public GenerateMerchantApiKeyHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<GenerateMerchantApiKeyCommand> validator,
        ILogger<GenerateMerchantApiKeyHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<GenerateMerchantApiKeyResponse>> Handle(GenerateMerchantApiKeyCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(GenerateMerchantApiKeyCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdWithApiKeysAsync(command.MerchantId, cancellationToken);
        if (merchant is null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        if (!command.Caller.CanAccess(merchant.OwnerId))
            return MerchantErrors.Unauthorized();

        if (!command.Caller.EmailVerified)
            return MerchantErrors.EmailNotVerified();

        if (merchant.Status != MerchantStatus.Active)
            return MerchantErrors.MerchantNotActive();

        var (plainTextKey, prefix, keyHash) = GenerateKey(command.Environment);
        var apiKey = merchant.GenerateApiKey(keyHash, prefix, command.Environment);

        await _repository.UpdateAsync(merchant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GenerateMerchantApiKeyResponse(apiKey.Id, plainTextKey, apiKey.Environment, apiKey.CreatedAt);
    }

    private static (string PlainTextKey, string Prefix, string KeyHash) GenerateKey(string environment)
    {
        var prefix = environment == "live" ? "sk_live_" : "sk_test_";
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var plainTextKey = prefix + secret;
        var keyHash = ApiKeyHasher.Hash(plainTextKey);
        return (plainTextKey, prefix, keyHash);
    }
}
