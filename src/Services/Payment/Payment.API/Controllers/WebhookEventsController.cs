using System.Globalization;
using BuildingBlocks.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.Features.Command.ReplayWebhookEvent;
using Payment.Application.Features.Command.SendTestWebhookEvent;
using Payment.Application.Features.Queries.ListWebhookEvents;

namespace Payment.API.Controllers;

[Authorize]
public class WebhookEventsController : BaseApiController
{
    /// <summary>
    /// List webhook delivery events for a merchant (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WebhookEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid merchantId,
        [FromServices] ListWebhookEventsHandler handler,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListWebhookEventsQuery(merchantId, normalizedSkip, normalizedTake, User.ToCallerContext()));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }

    /// <summary>
    /// Replay a webhook delivery event (resets it to Pending for redelivery).
    /// </summary>
    [HttpPost("{id:guid}/replay")]
    [ProducesResponseType(typeof(WebhookEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Replay(
        Guid merchantId,
        Guid id,
        [FromServices] ReplayWebhookEventHandler handler)
    {
        var result = await handler.Handle(new ReplayWebhookEventCommand(merchantId, id, User.ToCallerContext()));
        return result.ToActionResult();
    }

    /// <summary>
    /// Send a test webhook event to the merchant's configured endpoint.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(WebhookEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendTest(
        Guid merchantId,
        [FromServices] SendTestWebhookEventHandler handler)
    {
        var result = await handler.Handle(new SendTestWebhookEventCommand(merchantId, "test.event", null, User.ToCallerContext()));
        return result.ToActionResult();
    }
}
