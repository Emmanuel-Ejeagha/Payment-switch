using BuildingBlocks.Shared.Results;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.Tokens;

public class RevokeRefreshTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeRefreshTokenHandler> _logger;

    public RevokeRefreshTokenHandler(IUserRepository userRepository, ITokenService tokenService, IUnitOfWork unitOfWork, ILogger<RevokeRefreshTokenHandler> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeRefreshTokenCommand command, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for user {UserId}", nameof(RevokeRefreshTokenCommand), userId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(userId);

        user.RevokeRefreshToken(_tokenService.HashRefreshToken(command.RefreshToken));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
