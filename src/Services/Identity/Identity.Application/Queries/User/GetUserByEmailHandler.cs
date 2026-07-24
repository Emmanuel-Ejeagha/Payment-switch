using BuildingBlocks.Shared.Results;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Queries.User;

public class GetUserByEmailHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByEmailHandler> _logger;

    public GetUserByEmailHandler(IUserRepository userRepository, ILogger<GetUserByEmailHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetUserByEmailQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(GetUserByEmailQuery), query.Email);
        var user = await _userRepository.GetByEmailAsync(query.Email, cancellationToken);
        if (user == null)
            return IdentityErrors.InvalidCredentials;

        return new UserDto(user.Id, user.Email.Value, user.FullName.Value, user.IsActive, user.Roles.ToList());
    }
}