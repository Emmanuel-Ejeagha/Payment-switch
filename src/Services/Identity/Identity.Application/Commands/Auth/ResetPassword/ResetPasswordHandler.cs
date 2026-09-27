using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.ResetPassword;

public class ResetPasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationTokenFactory _tokenFactory;
    private readonly IValidator<ResetPasswordCommand> _validator;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailVerificationTokenFactory tokenFactory,
        IValidator<ResetPasswordCommand> validator,
        ILogger<ResetPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenFactory = tokenFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(ResetPasswordCommand), DataMasker.MaskEmail(command.Email));
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
            return IdentityErrors.InvalidPasswordResetToken;

        // A deactivated account must stay unusable even with a previously
        // issued token; reactivation is an explicit admin action.
        if (!user.IsActive)
            return new Error("Identity.UserInactive", "User account is deactivated.");

        var result = user.ResetPassword(_tokenFactory.Hash(command.Token), _passwordHasher.Hash(command.NewPassword));
        switch (result)
        {
            case PasswordResetResult.Success:
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Password reset for {Identifier}", DataMasker.MaskEmail(command.Email));
                return Result.Success();
            case PasswordResetResult.TokenExpired:
                return IdentityErrors.PasswordResetTokenExpired;
            default:
                return IdentityErrors.InvalidPasswordResetToken;
        }
    }
}
