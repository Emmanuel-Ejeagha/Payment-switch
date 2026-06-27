using Identity.API.Extensions;
using Identity.Application.Queries.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(
        [FromServices] GetUserByIdHandler handler)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        var result = await handler.Handle(new GetUserByIdQuery(Guid.Parse(userId)));
        return result.ToActionResult();
    }
}