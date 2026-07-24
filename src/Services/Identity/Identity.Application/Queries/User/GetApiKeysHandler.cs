using BuildingBlocks.Shared.Results;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Queries.User;

public class GetApiKeysHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetApiKeysHandler> _logger;

    public GetApiKeysHandler(IUserRepository userRepository, ILogger<GetApiKeysHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<List<ApiKeyDto>>> Handle(GetApiKeysQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Identifier}", nameof(GetApiKeysQuery), query.UserId);
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(query.UserId);

        var apiKeys = await _userRepository.GetApiKeysByUserIdAsync(query.UserId, cancellationToken);
        return apiKeys;
    }
}