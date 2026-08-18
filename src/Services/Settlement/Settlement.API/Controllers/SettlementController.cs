using System.Globalization;
using BuildingBlocks.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Settlement.API.Extensions;
using Settlement.Application.DTOs;
using Settlement.Application.Features.Command.TriggerSettlement;
using Settlement.Application.Features.Queries.GetSettlementBatch;
using Settlement.Application.Features.Queries.ListSettlementBatches;

namespace Settlement.API.Controllers;

[Authorize(Roles = "Admin")]
public class SettlementController : BaseApiController
{
    /// <summary>
    /// Manually trigger a settlement batch for a given date.
    /// </summary>
    [HttpPost("trigger")]
    [ProducesResponseType(typeof(TriggerSettlementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Trigger(
        [FromBody] TriggerSettlementCommand command,
        [FromServices] TriggerSettlementHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get a settlement batch by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SettlementBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetSettlementBatchHandler handler)
    {
        var result = await handler.Handle(new GetSettlementBatchQuery(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// List settlement batches with optional date filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SettlementBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromServices] ListSettlementBatchesHandler handler,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListSettlementBatchesQuery(from, to, normalizedSkip, normalizedTake));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }
}