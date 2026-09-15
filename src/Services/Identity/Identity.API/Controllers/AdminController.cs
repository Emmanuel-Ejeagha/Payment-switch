using Identity.API.Extensions;
using Identity.Application.Commands.Admin;
using Identity.Application.Commands.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize(Roles = "Admin")]
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

    /// <summary>
    /// Suspend a user account (admin only). Deactivation also revokes every
    /// refresh token, so the account cannot mint new access tokens.
    /// </summary>
    /// <param name="id">The target user ID.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK if suspended, or an error response.</returns>
    [HttpPost("users/{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SuspendUser(
        Guid id,
        [FromServices] SuspendUserHandler handler)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId))
            return Unauthorized();
        var result = await handler.Handle(new SuspendUserCommand(adminUserId, id));
        return result.ToActionResult();
    }

    /// <summary>
    /// Re-activate a suspended user account (admin only).
    /// </summary>
    /// <param name="id">The target user ID.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK if re-activated, or an error response.</returns>
    [HttpPost("users/{id:guid}/unsuspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnsuspendUser(
        Guid id,
        [FromServices] UnsuspendUserHandler handler)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId))
            return Unauthorized();
        var result = await handler.Handle(new UnsuspendUserCommand(adminUserId, id));
        return result.ToActionResult();
    }
}