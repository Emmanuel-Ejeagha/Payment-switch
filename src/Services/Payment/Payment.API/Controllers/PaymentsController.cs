using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.AuthorizePayment;
using Payment.Application.Features.Command.CapturePayment;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Features.Command.RefundPayment;
using Payment.Application.Features.Command.VoidPayment;
using Payment.Application.Features.Queries.GetPaymentIntentById;
using Payment.Application.Features.Queries.ListPaymentIntentsByMerchant;

namespace Payment.API.Controllers;

[Authorize]
public class PaymentsController : BaseApiController
{
    /// <summary>
    /// Create a new payment intent.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentIntentCommand command,
        [FromServices] CreatePaymentIntentHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Authorize a pending payment intent.
    /// </summary>
    [HttpPost("{id:guid}/authorize")]
    [ProducesResponseType(typeof(AuthorizePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Authorize(
        Guid id,
        [FromBody] AuthorizePaymentCommand command,
        [FromServices] AuthorizePaymentHandler handler)
    {
        command = new AuthorizePaymentCommand(id, command.CardLastFour, command.CardBrand);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Capture an authorized payment (full or partial).
    /// </summary>
    [HttpPost("{id:guid}/capture")]
    [ProducesResponseType(typeof(CapturePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Capture(
        Guid id,
        [FromBody] CapturePaymentCommand command,
        [FromServices] CapturePaymentHandler handler)
    {
        command = new CapturePaymentCommand(id, command.Amount);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Void an authorized payment.
    /// </summary>
    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(VoidPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Void(
        Guid id,
        [FromServices] VoidPaymentHandler handler)
    {
        var result = await handler.Handle(new VoidPaymentCommand(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// Refund a captured payment (full or partial).
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(RefundPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refund(
        Guid id,
        [FromBody] RefundPaymentCommand command,
        [FromServices] RefundPaymentHandler handler)
    {
        command = new RefundPaymentCommand(id, command.Amount);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get payment intent details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentIntentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetPaymentIntentByIdHandler handler)
    {
        var result = await handler.Handle(new GetPaymentIntentByIdQuery(id));
        return result.ToActionResult();
    }

    /// <summary>
    /// List payment intents for a merchant (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentIntentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid merchantId,
        [FromServices] ListPaymentIntentsByMerchantHandler handler,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10)
    {
        var result = await handler.Handle(new ListPaymentIntentsByMerchantQuery(merchantId, skip, take));
        return result.ToActionResult();
    }
}