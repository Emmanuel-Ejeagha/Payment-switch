using BuildingBlocks.Shared.Results;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Commands.Role;

public class AssignRoleHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<AssignRoleCommand> _validator;

    public AssignRoleHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, IValidator<AssignRoleCommand> validator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<Result> Handle(AssignRoleCommand command, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var adminUser = await _userRepository.GetByIdAsync(command.AdminUserId, cancellationToken);
        if (adminUser == null)
            return IdentityErrors.UserNotFound(command.AdminUserId);

        if (!adminUser.Roles.Contains("Admin"))
            return new Error("Identity.NotAuthorized", "Only admins can assign roles.");

        var targetUser = await _userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);
        if (targetUser == null)
            return IdentityErrors.UserNotFound(command.TargetUserId);

        targetUser.AddRole(command.Role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
