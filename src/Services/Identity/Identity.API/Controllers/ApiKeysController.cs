using Identity.API.Extensions;
using Identity.Application.Commands.ApiKey;
using Identity.Application.DTOs;
using Identity.Application.Queries.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize]
[Produces("application/json")]
public class ApiKeysController : BaseApiController
{
    /// <summary>
    /// Generate a new API key for the authenticated user.
    /// </summary>
    /// <param name="command">The API key environment (e.g., "live" or "test").</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>The newly created API key details (plain‑text key shown only once).</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

    /// <summary>
    /// Get all API keys for the authenticated user.
    /// </summary>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>A list of API key metadata (the actual secret is never returned).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetApiKeys(
        [FromServices] GetApiKeysHandler handler)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var result = await handler.Handle(new GetApiKeysQuery(Guid.Parse(userId)));
        return result.ToActionResult();
    }

    /// <summary>
    /// Revoke a specific API key.
    /// </summary>
    /// <param name="keyId">The ID of the API key to revoke.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK if revoked, 404 if the key was not found.</returns>
    [HttpDelete("{keyId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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