using Identity.API.Extensions;
using Identity.Application.Commands.Role;
using BuildingBlocks.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize(Roles = RolePolicies.AdminOnly)]
[Produces("application/json")]
[EnableRateLimiting("Strict")]
public class AdminController : BaseApiController
{
    /// <summary>
    /// Assign a role to a target user (admin only).
    /// </summary>
    /// <param name="command">The target user ID and the role to assign.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK if the role was assigned, or an error response.</returns>
    [HttpPost("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRole(
        [FromBody] AssignRoleCommand command,
        [FromServices] AssignRoleHandler handler)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId))
            return Unauthorized();
        command = new AssignRoleCommand(adminUserId, command.TargetUserId, command.Role);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }
}