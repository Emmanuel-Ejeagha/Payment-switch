using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.Register;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<RegisterUserCommand> validator,
        ILogger<RegisterUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(RegisterUserCommand), command.Email);
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        if (await _userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return IdentityErrors.EmailAlreadyInUse(command.Email);

        var email = new Email(command.Email);
        var passwordHash = _passwordHasher.Hash(command.Password);
        var fullName = new FullName(command.FullName);

        var user = new User(Guid.NewGuid(), email, passwordHash, fullName);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(user.DomainEvents, cancellationToken);

        return new RegisterUserResponse(user.Id);
    }
}
