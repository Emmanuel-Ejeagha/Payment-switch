using BuildingBlocks.Shared.Results;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.Tokens;

public class RevokeRefreshTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeRefreshTokenHandler> _logger;

    public RevokeRefreshTokenHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<RevokeRefreshTokenHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName}", nameof(RevokeRefreshTokenCommand));
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(command.UserId);

        user.RevokeRefreshToken(command.RefreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}