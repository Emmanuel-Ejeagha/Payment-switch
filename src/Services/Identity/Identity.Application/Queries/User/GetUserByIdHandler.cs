using BuildingBlocks.Shared.Results;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Queries.User;

public class GetUserByIdHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByIdHandler> _logger;

    public GetUserByIdHandler(IUserRepository userRepository, ILogger<GetUserByIdHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(GetUserByIdQuery), query.UserId);
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(query.UserId);

        return new UserDto(user.Id, user.Email.Value, user.FullName.Value, user.IsActive, user.Roles.ToList());
    }
}