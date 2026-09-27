using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.ChangePassword;

public class ChangePasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<ChangePasswordCommand> _validator;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IValidator<ChangePasswordCommand> validator,
        ILogger<ChangePasswordHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangePasswordCommand command, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for user {UserId}", nameof(ChangePasswordCommand), userId);
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(userId);

        if (!_passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
            return IdentityErrors.InvalidCurrentPassword;

        user.ChangePassword(_passwordHasher.Hash(command.NewPassword));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}", userId);
        return Result.Success();
    }
}
