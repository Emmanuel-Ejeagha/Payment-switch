using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.Login;

public class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<LoginCommand> _validator;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService, IUnitOfWork unitOfWork, IValidator<LoginCommand> validator, ILogger<LoginHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(LoginCommand), DataMasker.MaskEmail(command.Email));
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (user == null)
            return IdentityErrors.InvalidCredentials;

        if (user.IsLockedOut(DateTime.UtcNow))
        {
            _logger.LogWarning("Login rejected for locked user {UserId}", user.Id);
            return IdentityErrors.AccountLocked;
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Failed login for user {UserId}; failed attempts now {Count}", user.Id, user.AccessFailedCount);
            return IdentityErrors.InvalidCredentials;
        }

        if (!user.IsActive)
            return new Error("Identity.UserInactive", "User account is deactivated.");

        user.ResetAccessFailedCount();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresIn = _tokenService.AccessTokenExpirationSeconds;

        user.AddRefreshToken(_tokenService.HashRefreshToken(refreshToken), DateTime.UtcNow.AddDays(7));
        user.EnforceRefreshTokenCap();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshToken, expiresIn, user.EmailConfirmed);
    }
}