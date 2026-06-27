using Identity.API.Extensions;
using Identity.Application.Commands.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : BaseApiController
{
    [HttpPost("roles")]
    public async Task<IActionResult> AssignRole(
        [FromBody] AssignRoleCommand command,
        [FromServices] AssignRoleHandler handler)
    {
        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (adminUserId is null) return Unauthorized();
        command = new AssignRoleCommand(Guid.Parse(adminUserId), command.TargetUserId, command.Role);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }
}