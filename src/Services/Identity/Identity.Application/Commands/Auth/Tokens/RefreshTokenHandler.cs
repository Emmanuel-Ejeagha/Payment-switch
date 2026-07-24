using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Identity.Application.Interfaces;
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
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<RefreshTokenCommand> _validator;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(IUserRepository userRepository, ITokenService tokenService, IUnitOfWork unitOfWork, IDomainEventDispatcher dispatcher, IValidator<RefreshTokenCommand> validator, ILogger<RefreshTokenHandler> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(RefreshTokenCommand), command.RefreshToken);
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var user = await _userRepository.FindByRefreshTokenAsync(command.RefreshToken, cancellationToken);
        if (user == null)
            return new Error("Identity.InvalidRefreshToken", "Refresh token not found.");

        var token = user.RefreshTokens.FirstOrDefault(t => t.Value == command.RefreshToken);
        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            return new Error("Identity.RefreshTokenInvalidOrExpired", "Refresh token is invalid or expired.");

        user.RevokeRefreshToken(command.RefreshToken);
        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.AddRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(user.DomainEvents, cancellationToken);

        return new RefreshTokenResponse(newAccessToken, newRefreshToken, 3600);
    }
}
