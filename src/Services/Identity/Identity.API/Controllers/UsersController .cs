using Identity.API.Extensions;
using Identity.Application.DTOs;
using Identity.Application.Queries.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize]
[Produces("application/json")]
public class UsersController : BaseApiController
{
    /// <summary>
    /// Get the profile of the currently authenticated user.
    /// </summary>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>User profile information.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(
        [FromServices] GetUserByIdHandler handler)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var result = await handler.Handle(new GetUserByIdQuery(Guid.Parse(userId)));
        return result.ToActionResult();
    }
}