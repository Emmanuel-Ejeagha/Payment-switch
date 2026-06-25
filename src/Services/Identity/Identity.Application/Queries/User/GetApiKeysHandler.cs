using BuildingBlocks.Shared.Results;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.DomainErrors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Identity.Application.Queries.User;

public class GetApiKeysHandler
{
    private readonly IUserRepository _userRepository;

    public GetApiKeysHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<List<ApiKeyDto>>> Handle(GetApiKeysQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user == null)
            return IdentityErrors.UserNotFound(query.UserId);

        var keys = user.ApiKeys.Select(k => new ApiKeyDto(k.Id, k.Environment, k.CreatedAt, k.RevokedAt)).ToList();
        return keys;
    }
}