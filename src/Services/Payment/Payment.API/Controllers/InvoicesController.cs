using System.Globalization;
using BuildingBlocks.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.DTOs;
using Payment.Application.Features.Queries.ListInvoices;

namespace Payment.API.Controllers;

[Authorize]
public class InvoicesController : BaseApiController
{
    /// <summary>
    /// List a merchant's invoices (paginated), optionally filtered by subscription.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid merchantId,
        [FromServices] ListInvoicesHandler handler,
        [FromQuery] Guid? subscriptionId = null,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListInvoicesQuery(merchantId, subscriptionId, normalizedSkip, normalizedTake));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }
}
