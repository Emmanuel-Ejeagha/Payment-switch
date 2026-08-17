using Merchant.API.Contracts;
using Merchant.API.Extensions;
using Merchant.Application.DTOs;
using Merchant.Application.Features.Commands.ActivateMerchant;
using Merchant.Application.Features.Commands.ApproveMerchant;
using Merchant.Application.Features.Commands.OnboardMerchant;
using Merchant.Application.Features.Commands.ReactivateMerchant;
using Merchant.Application.Features.Commands.RejectMerchant;
using Merchant.Application.Features.Commands.RevokeMerchantApiKey;
using Merchant.Application.Features.Commands.RotateWebhookSecret;
using Merchant.Application.Features.Commands.SuspendMerchant;
using Merchant.Application.Features.Commands.UpdateContactDetails;
using Merchant.Application.Features.Commands.UpdateMerchantConfig;
using Merchant.Application.Features.Commands.UpdateSettlementInfo;
using Merchant.Application.Features.Queries.GetMerchantByEmail;
using Merchant.Application.Features.Queries.GetMerchantById;
using Merchant.Application.Features.Queries.ListMerchants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace Merchant.API.Controllers;

[Authorize]
[Produces("application/json")]
[EnableRateLimiting("Strict")]
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
    [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetMerchantByIdHandler handler)
    {
        var result = await handler.Handle(new GetMerchantByIdQuery(id, User.ToCallerContext()));
        return result.ToActionResult();
    }

    /// <summary>
    /// Retrieve a merchant by email (authenticated users only).
    /// </summary>
    /// <param name="email">Merchant email address.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>Merchant details, or 404 if not found.</returns>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEmail(
        string email,
        [FromServices] GetMerchantByEmailHandler handler)
    {
        var result = await handler.Handle(new GetMerchantByEmailQuery(email, User.ToCallerContext()));
        return result.ToActionResult();
    }

    /// <summary>
    /// Activate an approved merchant (admin only).
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
    /// Approve a pending merchant (admin only).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if approved, 400 if transition invalid, 404 if not found.</returns>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromServices] ApproveMerchantHandler handler)
    {
        var result = await handler.Handle(new ApproveMerchantCommand(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// Reject a pending merchant with a reason (admin only).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="request">Rejection reason.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if rejected, 400 if transition invalid, 404 if not found.</returns>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectMerchantRequest request,
        [FromServices] RejectMerchantHandler handler)
    {
        var result = await handler.Handle(new RejectMerchantCommand(id, request.Reason));
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
    /// Reactivate a suspended merchant (admin only).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if reactivated, 400 if transition invalid, 404 if not found.</returns>
    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(
        Guid id,
        [FromServices] ReactivateMerchantHandler handler)
    {
        var result = await handler.Handle(new ReactivateMerchantCommand(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// Update merchant settlement/bank details (owner or admin).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="request">Settlement details.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if updated, 400 if invalid, 404 if not found.</returns>
    [HttpPut("{id:guid}/settlement-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettlementInfo(
        Guid id,
        [FromBody] UpdateSettlementInfoRequest request,
        [FromServices] UpdateSettlementInfoHandler handler)
    {
        var command = new UpdateSettlementInfoCommand(
            id,
            request.BankAccountName,
            request.BankAccountNumber,
            request.BankName,
            request.SettlementCurrency,
            request.SettlementSchedule,
            User.ToCallerContext());
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update merchant contact details (owner or admin).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="request">Contact details.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if updated, 400 if invalid, 404 if not found.</returns>
    [HttpPut("{id:guid}/contact-details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContactDetails(
        Guid id,
        [FromBody] UpdateContactDetailsRequest request,
        [FromServices] UpdateContactDetailsHandler handler)
    {
        var command = new UpdateContactDetailsCommand(
            id,
            request.Phone,
            request.Address,
            request.ContactPerson,
            User.ToCallerContext());
        var result = await handler.Handle(command);
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
        [FromBody] UpdateMerchantConfigurationRequest request,
        [FromServices] UpdateMerchantConfigurationHandler handler)
    {
        var command = new UpdateMerchantConfigurationCommand(id, request.WebhookUrl, request.PaymentMethods, request.AutoCapture, User.ToCallerContext());
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Rotate the merchant webhook signing secret (authenticated users only).
    /// </summary>
    /// <param name="id">Merchant unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>The new webhook secret.</returns>
    [HttpPost("{id:guid}/webhook-secret/rotate")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateWebhookSecret(
        Guid id,
        [FromServices] RotateWebhookSecretHandler handler)
    {
        var result = await handler.Handle(new RotateWebhookSecretCommand(id, User.ToCallerContext()));
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