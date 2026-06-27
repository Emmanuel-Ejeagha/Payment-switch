using Identity.API.Extensions;
using Identity.Application.Commands.ApiKey;
using Identity.Application.Queries.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize]
public class ApiKeysController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateApiKeyCommand command,
        [FromServices] GenerateApiKeyHandler handler)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        command = new GenerateApiKeyCommand(Guid.Parse(userId), command.Environment);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetApiKeys(
        [FromServices] GetApiKeysHandler handler)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var result = await handler.Handle(new GetApiKeysQuery(Guid.Parse(userId)));
        return result.ToActionResult();
    }

    [HttpDelete("{keyId:guid}")]
    public async Task<IActionResult> Revoke(
        Guid keyId,
        [FromServices] RevokeApiKeyHandler handler)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var result = await handler.Handle(new RevokeApiKeyCommand(Guid.Parse(userId), keyId));
        return result.ToActionResult();
    }
}