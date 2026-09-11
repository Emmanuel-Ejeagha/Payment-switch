using BuildingBlocks.Shared.Results;
using FluentValidation;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Commands.Auth.Tokens;

public class RefreshTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RefreshTokenCommand> _validator;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(IUserRepository userRepository, ITokenService tokenService, IUnitOfWork unitOfWork, IValidator<RefreshTokenCommand> validator, ILogger<RefreshTokenHandler> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName}", nameof(RefreshTokenCommand));
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var tokenHash = _tokenService.HashRefreshToken(command.RefreshToken);
        var user = await _userRepository.FindByRefreshTokenAsync(tokenHash, cancellationToken);
        if (user == null)
            return new Error("Identity.InvalidRefreshToken", "Refresh token not found.");

        var token = user.RefreshTokens.FirstOrDefault(t => t.Value == tokenHash);
        if (token == null || token.ExpiresAt < DateTime.UtcNow)
            return new Error("Identity.RefreshTokenInvalidOrExpired", "Refresh token is invalid or expired.");

        if (token.IsRevoked)
        {
            _logger.LogWarning("Refresh token reuse detected for user {UserId}. Revoking all refresh tokens.", user.Id);
            user.RevokeAllRefreshTokens();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new Error("Identity.RefreshTokenReuseDetected", "Refresh token reuse detected. All refresh tokens revoked.");
        }

        if (user.IsLockedOut(DateTime.UtcNow))
        {
            _logger.LogWarning("Refresh rejected for locked user {UserId}", user.Id);
            return IdentityErrors.AccountLocked;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Refresh rejected for deactivated user {UserId}", user.Id);
            return new Error("Identity.UserInactive", "User account is deactivated.");
        }

        user.RevokeRefreshToken(tokenHash);
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.AddRefreshToken(_tokenService.HashRefreshToken(newRefreshToken), DateTime.UtcNow.AddDays(7));
        user.EnforceRefreshTokenCap();
        await _userRepository.PruneRefreshTokensAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(newAccessToken, newRefreshToken, _tokenService.AccessTokenExpirationSeconds);
    }
}
