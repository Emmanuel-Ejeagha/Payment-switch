using System.Globalization;
using BuildingBlocks.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CancelSubscription;
using Payment.Application.Features.Command.CollectSubscriptionCycle;
using Payment.Application.Features.Command.CreateSubscription;
using Payment.Application.Features.Queries.GetSubscriptionById;
using Payment.Application.Features.Queries.ListInvoices;
using Payment.Application.Features.Queries.ListSubscriptionsByMerchant;

namespace Payment.API.Controllers;

[Authorize]
public class SubscriptionsController : BaseApiController
{
    /// <summary>
    /// Subscribe a customer to a plan using a stored card token.
    /// The first cycle is billed by the billing worker on the next pass.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSubscriptionCommand command,
        [FromServices] CreateSubscriptionHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Retrieve a single subscription.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetSubscriptionByIdHandler handler)
    {
        var result = await handler.Handle(new GetSubscriptionByIdQuery(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// List a merchant's subscriptions (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid merchantId,
        [FromServices] ListSubscriptionsByMerchantHandler handler,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListSubscriptionsByMerchantQuery(merchantId, normalizedSkip, normalizedTake));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }

    /// <summary>
    /// Cancel a subscription immediately, or at the end of the current period.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] CancelSubscriptionHandler handler,
        [FromQuery] bool atPeriodEnd = false)
    {
        var result = await handler.Handle(new CancelSubscriptionCommand(id, atPeriodEnd));
        return result.ToActionResult();
    }

    /// <summary>
    /// Force a collection attempt for the current billing period instead of
    /// waiting for the billing worker. Safe to call repeatedly.
    /// </summary>
    [HttpPost("{id:guid}/collect")]
    [ProducesResponseType(typeof(CollectSubscriptionCycleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Collect(
        Guid id,
        [FromServices] CollectSubscriptionCycleHandler handler)
    {
        var result = await handler.Handle(new CollectSubscriptionCycleCommand(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// List invoices for a subscription.
    /// </summary>
    [HttpGet("{id:guid}/invoices")]
    [ProducesResponseType(typeof(List<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvoices(
        Guid id,
        [FromQuery] Guid merchantId,
        [FromServices] ListInvoicesHandler handler,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListInvoicesQuery(merchantId, id, normalizedSkip, normalizedTake));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }
}
