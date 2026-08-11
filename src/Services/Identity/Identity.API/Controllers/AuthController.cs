using Identity.API.Extensions;
using Identity.Application.Commands.Auth.Login;
using Identity.Application.Commands.Auth.Register;
using Identity.Application.Commands.Auth.ResendVerification;
using Identity.Application.Commands.Auth.Tokens;
using Identity.Application.Commands.Auth.VerifyEmail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers;

[Produces("application/json")]
[EnableRateLimiting("Strict")]
public class AuthController : BaseApiController
{
    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="command">Email, password, and full name.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK with the new user ID, or validation/conflict errors.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        [FromServices] RegisterUserHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Confirm an email address with the single-use token from the verification link.
    /// </summary>
    /// <param name="command">Email address and the verification token.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK when verified, or invalid/expired token errors.</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailCommand command,
        [FromServices] VerifyEmailHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Issue a new verification token and email it to the account (rate-limited).
    /// The previous token, if any, is invalidated.
    /// </summary>
    /// <param name="command">Email address of the unverified account.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK when a new verification email was issued.</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationCommand command,
        [FromServices] ResendVerificationHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Log in with email and password to obtain JWT access and refresh tokens.
    /// </summary>
    /// <param name="command">Email and password.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>Access token, refresh token, and expiry.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        [FromServices] LoginHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Exchange a valid refresh token for a new access/refresh token pair.
    /// </summary>
    /// <param name="command">The existing refresh token.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>New access token and refresh token.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        [FromServices] RefreshTokenHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Revoke a specific refresh token belonging to the authenticated user.
    /// </summary>
    /// <param name="command">The refresh token to revoke.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 OK if revoked, or an error if the token was not found.</returns>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeRefreshTokenCommand command,
        [FromServices] RevokeRefreshTokenHandler handler)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await handler.Handle(command, Guid.Parse(userId));
        return result.ToActionResult();
    }
}