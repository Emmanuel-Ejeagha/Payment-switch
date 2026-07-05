using BuildingBlocks.Shared.Results;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;

namespace Identity.Application.Commands.Auth.Tokens;

public class RevokeRefreshTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeRefreshTokenHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(command.UserId);

        user.RevokeRefreshToken(command.RefreshToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}