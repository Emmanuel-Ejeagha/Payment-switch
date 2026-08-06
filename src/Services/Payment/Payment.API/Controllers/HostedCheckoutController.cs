using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CheckoutPayment;
using Payment.Application.Features.Command.CheckoutTokenize;
using Payment.Application.Features.Queries.GetPaymentLinkByCode;

namespace Payment.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v1/checkout")]
[Produces("application/json")]
public class HostedCheckoutController : ControllerBase
{
    /// <summary>
    /// Resolve a payment link by its public code (no authentication required).
    /// </summary>
    [HttpGet("links/{code}")]
    [ProducesResponseType(typeof(PaymentLinkDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLink(
        string code,
        [FromServices] GetPaymentLinkByCodeHandler handler)
    {
        var result = await handler.Handle(new GetPaymentLinkByCodeQuery(code));
        return result.ToActionResult();
    }

    /// <summary>
    /// Tokenize a card for a payment link (no authentication required).
    /// </summary>
    [HttpPost("links/{code}/tokenize")]
    [ProducesResponseType(typeof(CheckoutTokenizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Tokenize(
        string code,
        [FromBody] CheckoutTokenizeRequest request,
        [FromServices] CheckoutTokenizeHandler handler)
    {
        var command = new CheckoutTokenizeCommand(code, request.CardNumber, request.ExpiryMonth, request.ExpiryYear);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Pay a payment link (no authentication required).
    /// </summary>
    [HttpPost("links/{code}/pay")]
    [ProducesResponseType(typeof(CheckoutPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pay(
        string code,
        [FromBody] CheckoutPaymentRequest request,
        [FromServices] CheckoutPaymentHandler handler)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? request.IdempotencyKey;
        // A missing idempotency key is rejected by CheckoutPaymentCommandValidator
        // (NotEmpty); the null-forgiving operator documents that intent here.
        var command = new CheckoutPaymentCommand(code, request.CardToken, idempotencyKey!, request.SecurityCode);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }
}

public record CheckoutTokenizeRequest(string CardNumber, int ExpiryMonth, int ExpiryYear);

/// <param name="SecurityCode">
/// CVC/CVV/CID. Sent on pay rather than tokenize because it is only valid for the
/// single authorization it accompanies and is never stored (PCI DSS 3.2).
/// </param>
/// <param name="CardToken">Token minted by the tokenize call.</param>
/// <param name="IdempotencyKey">Fallback when the Idempotency-Key header is absent.</param>
public record CheckoutPaymentRequest(string CardToken, string? IdempotencyKey = null, string? SecurityCode = null);
