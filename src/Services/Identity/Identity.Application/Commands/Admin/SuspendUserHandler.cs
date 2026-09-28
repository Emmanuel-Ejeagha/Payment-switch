using BuildingBlocks.Shared.Results;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Admin;

public class SuspendUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SuspendUserCommand> _validator;
    private readonly ILogger<SuspendUserHandler> _logger;

    public SuspendUserHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IValidator<SuspendUserCommand> validator,
        ILogger<SuspendUserHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(SuspendUserCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName}", nameof(SuspendUserCommand));
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var adminUser = await _userRepository.GetByIdAsync(command.AdminUserId, cancellationToken);
        if (adminUser == null)
            return IdentityErrors.UserNotFound(command.AdminUserId);

        if (!adminUser.Roles.Contains("Admin"))
            return new Error("Identity.NotAuthorized", "Only admins can suspend users.");

        if (command.AdminUserId == command.TargetUserId)
            return new Error("Identity.CannotSuspendSelf", "Admins cannot suspend their own account.");

        var targetUser = await _userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (targetUser == null)
            return IdentityErrors.UserNotFound(command.TargetUserId);

        targetUser.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
