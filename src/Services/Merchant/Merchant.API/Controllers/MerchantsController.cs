using Merchant.API.Extensions;
using Merchant.Application.DTOs;
using Merchant.Application.Features.Commands.ActivateMerchant;
using Merchant.Application.Features.Commands.OnboardMerchant;
using Merchant.Application.Features.Commands.SuspendMerchant;
using Merchant.Application.Features.Commands.UpdateMerchantConfig;
using Merchant.Application.Features.Queries.GetMerchantByEmail;
using Merchant.Application.Features.Queries.GetMerchantById;
using Merchant.Application.Features.Queries.ListMerchants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Merchant.API.Controllers;

[Authorize]
[Produces("application/json")]
public class MerchantsController : BaseApiController
{
    /// <summary>
    /// Onboard a new merchant (public endpoint – no authentication required).
    /// </summary>
    /// <param name="command">Business name and email address.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>New merchant ID, or validation/conflict errors.</returns>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OnboardMerchantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Onboard(
        [FromBody] OnboardMerchantCommand command,
        [FromServices] OnboardMerchantHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Retrieve a merchant by ID (authenticated users only).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>Merchant details, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [OutputCache(PolicyName = "CacheById")]
    [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetMerchantByIdHandler handler)
    {
        var result = await handler.Handle(new GetMerchantByIdQuery(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// Retrieve a merchant by email (authenticated users only).
    /// </summary>
    /// <param name="email">Merchant email address.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>Merchant details, or 404 if not found.</returns>
    [HttpGet("by-email/{email}")]
    [OutputCache(PolicyName = "CacheById")]
    [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEmail(
        string email,
        [FromServices] GetMerchantByEmailHandler handler)
    {
        var result = await handler.Handle(new GetMerchantByEmailQuery(email));
        return result.ToActionResult();
    }

    /// <summary>
    /// Activate a pending merchant (admin only).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if activated, 400 if transition invalid, 404 if not found.</returns>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        Guid id,
        [FromServices] ActivateMerchantHandler handler)
    {
        var result = await handler.Handle(new ActivateMerchantCommand(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// Suspend an active merchant (admin only).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if suspended, 400 if transition invalid, 404 if not found.</returns>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(
        Guid id,
        [FromServices] SuspendMerchantHandler handler)
    {
        var result = await handler.Handle(new SuspendMerchantCommand(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// Update merchant configuration – webhook URL and/or payment methods (authenticated users).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="command">New webhook URL and/or payment method list.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if updated, 400 if invalid transition, 404 if not found.</returns>
    [HttpPut("{id:guid}/configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConfiguration(
        Guid id,
        [FromBody] UpdateMerchantConfigurationCommand command,
        [FromServices] UpdateMerchantConfigurationHandler handler)
    {
        command = new UpdateMerchantConfigurationCommand(id, command.WebhookUrl, command.PaymentMethods);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// List merchants with paging (admin only).
    /// </summary>
    /// <param name="handler">Handler injected via DI.</param>
    /// <param name="skip">Number of records to skip (default 0).</param>
    /// <param name="take">Number of records to take (default 10).</param>
    /// <returns>A list of merchant DTOs.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<MerchantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromServices] ListMerchantsHandler handler,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10)
    {
        var result = await handler.Handle(new ListMerchantsQuery(skip, take));
        return result.ToActionResult();
    }
}