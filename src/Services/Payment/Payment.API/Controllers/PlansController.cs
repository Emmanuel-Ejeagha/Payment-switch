using System.Globalization;
using BuildingBlocks.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.ArchivePlan;
using Payment.Application.Features.Command.CreatePlan;
using Payment.Application.Features.Queries.ListPlansByMerchant;

namespace Payment.API.Controllers;

[Authorize]
public class PlansController : BaseApiController
{
    /// <summary>
    /// Create a billing plan (amount + recurring interval) for a merchant.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlanCommand command,
        [FromServices] CreatePlanHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// List a merchant's plans (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid merchantId,
        [FromServices] ListPlansByMerchantHandler handler,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListPlansByMerchantQuery(merchantId, normalizedSkip, normalizedTake));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }

    /// <summary>
    /// Archive a plan so no new subscriptions can be created against it.
    /// Existing subscriptions keep billing.
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(
        Guid id,
        [FromServices] ArchivePlanHandler handler)
    {
        var result = await handler.Handle(new ArchivePlanCommand(id));
        return result.ToActionResult();
    }
}
