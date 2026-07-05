using Identity.API.Extensions;
using Identity.Application.Commands.Auth.Login;
using Identity.Application.Commands.Auth.Register;
using Identity.Application.Commands.Auth.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

public class AuthController : BaseApiController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        [FromServices] RegisterUserHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        [FromServices] LoginHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        [FromServices] RefreshTokenHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeRefreshTokenCommand command,
        [FromServices] RevokeRefreshTokenHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }
}