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
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var result = await handler.Handle(new ListInvoicesQuery(merchantId, subscriptionId, skip, take));
        return result.ToActionResult();
    }
}
