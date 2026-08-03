using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Features.Queries.GetPaymentIntentById;

namespace Payment.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v1/payments")]
[Produces("application/json")]
public class PublicPaymentsController : ControllerBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PublicPaymentsController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Create a new payment intent (public API, authenticated with a secret key).
    /// </summary>
    /// <param name="request">Amount, currency, payment method and optional card details.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>The created payment intent.</returns>
    [HttpPost("intents")]
    [ProducesResponseType(typeof(PaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateIntent(
        [FromBody] PublicCreatePaymentIntentRequest request,
        [FromServices] CreatePaymentIntentHandler handler)
    {
        if (!_httpContextAccessor.HttpContext!.Items.TryGetValue("MerchantId", out var merchantIdObj) || merchantIdObj is not Guid merchantId)
            return Unauthorized();

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        var command = new CreatePaymentIntentCommand(
            merchantId,
            request.Amount,
            request.Currency,
            request.PaymentMethod,
            request.CardLastFour,
            request.CardBrand,
            idempotencyKey);

        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Retrieve a payment intent by ID (public API, authenticated with a secret key).
    /// </summary>
    /// <param name="id">Payment intent unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>The payment intent details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentIntentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIntent(
        Guid id,
        [FromServices] GetPaymentIntentByIdHandler handler)
    {
        if (!_httpContextAccessor.HttpContext!.Items.TryGetValue("MerchantId", out var merchantIdObj) || merchantIdObj is not Guid merchantId)
            return Unauthorized();

        var result = await handler.Handle(new GetPaymentIntentByIdQuery(id));
        if (result.IsSuccess && result.Value.MerchantId != merchantId)
            return NotFound();

        return result.ToActionResult();
    }
}

public record PublicCreatePaymentIntentRequest(
    long Amount,
    string Currency,
    string PaymentMethod,
    string? CardLastFour = null,
    string? CardBrand = null
);
