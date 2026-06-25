using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;

namespace Identity.Application.Commands.ApiKey;

public class GenerateApiKeyHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<GenerateApiKeyCommand> _validator;

    public GenerateApiKeyHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IDomainEventDispatcher dispatcher, IValidator<GenerateApiKeyCommand> validator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result<ApiKeyResponse>> Handle(GenerateApiKeyCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if(!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(command.UserId);

        var plainTextKey = GenerateRandomKey();
        var keyHash = HashKey(plainTextKey);
        var keyId = Guid.NewGuid();

        var apiKey = user.GenerateApiKey(keyId, keyHash, command.Environment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(user.DomainEvents, cancellationToken);

        return new ApiKeyResponse(keyId, plainTextKey, command.Environment, apiKey.CreatedAt);
    }

    private string GenerateRandomKey() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    private string HashKey(string key) => Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));
}