using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.VerifyEmail;

public class VerifyEmailHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailVerificationTokenFactory _tokenFactory;
    private readonly IValidator<VerifyEmailCommand> _validator;
    private readonly ILogger<VerifyEmailHandler> _logger;

    public VerifyEmailHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailVerificationTokenFactory tokenFactory,
        IValidator<VerifyEmailCommand> validator,
        ILogger<VerifyEmailHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenFactory = tokenFactory;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(VerifyEmailCommand), DataMasker.MaskEmail(command.Email));
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
        {
            // Respond identically for unknown addresses so the endpoint cannot be
            // used to enumerate which emails have accounts.
            _logger.LogWarning("Email verification requested for unknown {Identifier}", DataMasker.MaskEmail(command.Email));
            return Result.Success();
        }

        var result = user.VerifyEmail(_tokenFactory.Hash(command.Token));
        switch (result)
        {
            case EmailVerificationResult.Success:
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Email verified for {Identifier}", DataMasker.MaskEmail(command.Email));
                return Result.Success();
            case EmailVerificationResult.AlreadyConfirmed:
                return IdentityErrors.EmailAlreadyVerified;
            case EmailVerificationResult.TokenExpired:
                return IdentityErrors.VerificationTokenExpired;
            default:
                return IdentityErrors.InvalidVerificationToken;
        }
    }
}
