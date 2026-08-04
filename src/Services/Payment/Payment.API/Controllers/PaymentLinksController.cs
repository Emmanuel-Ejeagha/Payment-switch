using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.Features.Command.CreatePaymentLink;
using Payment.Application.Features.Queries.ListPaymentLinksByMerchant;

namespace Payment.API.Controllers;

[Authorize]
public class PaymentLinksController : BaseApiController
{
    /// <summary>
    /// Create a new payment link for a merchant.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentLinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentLinkCommand command,
        [FromServices] CreatePaymentLinkHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// List payment links for a merchant (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid merchantId,
        [FromServices] ListPaymentLinksByMerchantHandler handler,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var result = await handler.Handle(new ListPaymentLinksByMerchantQuery(merchantId, skip, take));
        return result.ToActionResult();
    }
}
