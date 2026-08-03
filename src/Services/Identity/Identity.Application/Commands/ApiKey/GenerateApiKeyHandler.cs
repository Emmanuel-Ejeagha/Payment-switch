using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Identity.Application.Commands.ApiKey;

public class GenerateApiKeyHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<GenerateApiKeyCommand> _validator;
    private readonly ILogger<GenerateApiKeyHandler> _logger;

    public GenerateApiKeyHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IDomainEventDispatcher dispatcher, IValidator<GenerateApiKeyCommand> validator, ILogger<GenerateApiKeyHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ApiKeyResponse>> Handle(GenerateApiKeyCommand query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(GenerateApiKeyCommand), query.UserId);
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(query.UserId);

        var plainTextKey = GenerateRandomKey(query.Environment);
        var keyHash = ApiKeyHasher.Hash(plainTextKey);

        var apiKey = user.GenerateApiKey(keyHash, query.Environment);
        await _userRepository.AddApiKeyAsync(user, apiKey, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(user.DomainEvents, cancellationToken);

        return new ApiKeyResponse(apiKey.Id, plainTextKey, query.Environment, apiKey.CreatedAt);
    }

    private string GenerateRandomKey(string environment)
    {
        var prefix = environment == "live" ? "sk_live_" : "sk_test_";
        return prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }
}