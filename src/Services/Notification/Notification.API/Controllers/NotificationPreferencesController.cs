using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.API.Extensions;
using Notification.Application.DTOs;
using Notification.Application.Features.Commands.UpdateNotificationPreference;
using Notification.Application.Features.Queries.GetNotificationPreferences;

namespace Notification.API.Controllers;

[Authorize]
[Route("api/v{version:apiVersion}/notifications")]
public class NotificationPreferencesController : BaseApiController
{
    /// <summary>
    /// List the authenticated user's notification preferences.
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(List<NotificationPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromServices] GetNotificationPreferencesHandler handler,
        CancellationToken cancellationToken)
    {
        var recipient = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(recipient))
            return Unauthorized();

        var result = await handler.Handle(new GetNotificationPreferencesQuery(recipient), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Create or update one of the authenticated user's notification preferences.
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        [FromBody] UpdateNotificationPreferenceRequest request,
        [FromServices] UpdateNotificationPreferenceHandler handler,
        CancellationToken cancellationToken)
    {
        var recipient = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(recipient))
            return Unauthorized();

        var command = new UpdateNotificationPreferenceCommand(recipient, request.Channel, request.EventType, request.Enabled);
        var result = await handler.Handle(command, cancellationToken);
        return result.ToActionResult();
    }
}

public record UpdateNotificationPreferenceRequest(string Channel, string EventType, bool Enabled);
