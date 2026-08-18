using System.Globalization;
using BuildingBlocks.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.API.Extensions;
using Notification.Application.DTOs;
using Notification.Application.Features.Queries.GetNotificationById;
using Notification.Application.Features.Queries.ListNotifications;

namespace Notification.API.Controllers;

[Authorize(Roles = "Admin")]
public class NotificationsController : BaseApiController
{
    /// <summary>
    /// Get a single notification by ID (Admin only).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetNotificationByIdHandler handler)
    {
        var result = await handler.Handle(new GetNotificationByIdQuery(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// List notifications with optional filters (Admin only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? recipient,
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromServices] ListNotificationsHandler handler,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListNotificationsQuery(recipient, channel, status, normalizedSkip, normalizedTake));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }
}