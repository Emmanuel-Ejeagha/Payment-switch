using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;

namespace Identity.Application.Commands.ApiKey;

public class RevokeApiKeyHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<RevokeApiKeyCommand> _validator;

    public RevokeApiKeyHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IDomainEventDispatcher dispatcher, IValidator<RevokeApiKeyCommand> validator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result> Handle(RevokeApiKeyCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(command.UserId);

        try
        {
            user.RevokeApiKey(command.KeyId);
        }
        catch (InvalidOperationException)
        {
            return IdentityErrors.ApiKeyNotFound(command.KeyId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(user.DomainEvents, cancellationToken);
        return Result.Success();
    }
}