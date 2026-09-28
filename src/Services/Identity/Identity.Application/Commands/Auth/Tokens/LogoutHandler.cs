using BuildingBlocks.Shared.Results;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.Auth.Tokens;

public class LogoutHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<LogoutHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for user {UserId}", nameof(LogoutCommand), command.UserId);
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(command.UserId);

        user.RevokeAllRefreshTokens();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
