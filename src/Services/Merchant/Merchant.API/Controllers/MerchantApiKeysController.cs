using Asp.Versioning;
using Merchant.API.Extensions;
using Merchant.Application.DTOs;
using Merchant.Application.Features.Commands.GenerateMerchantApiKey;
using Merchant.Application.Features.Commands.RevokeMerchantApiKey;
using Merchant.Application.Features.Queries.GetMerchantApiKeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Merchant.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/merchants/{merchantId:guid}/apikeys")]
[Produces("application/json")]
public class MerchantApiKeysController : ControllerBase
{
    /// <summary>
    /// Generate a new secret key for a merchant.
    /// </summary>
    /// <param name="merchantId">Merchant unique identifier.</param>
    /// <param name="command">The environment ("live" or "test") for the key.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>The newly generated secret key (plain-text shown only once).</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GenerateMerchantApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(
        Guid merchantId,
        [FromBody] GenerateMerchantApiKeyCommand command,
        [FromServices] GenerateMerchantApiKeyHandler handler)
    {
        var result = await handler.Handle(new GenerateMerchantApiKeyCommand(merchantId, command.Environment));
        return result.ToActionResult();
    }

    /// <summary>
    /// List secret keys for a merchant (secret values never returned).
    /// </summary>
    /// <param name="merchantId">Merchant unique identifier.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>A list of key metadata.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<MerchantApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApiKeys(
        Guid merchantId,
        [FromServices] GetMerchantApiKeysHandler handler)
    {
        var result = await handler.Handle(new GetMerchantApiKeysQuery(merchantId));
        return result.ToActionResult();
    }

    /// <summary>
    /// Revoke a specific secret key.
    /// </summary>
    /// <param name="merchantId">Merchant unique identifier.</param>
    /// <param name="keyId">The key to revoke.</param>
    /// <param name="handler">Handler injected via DI.</param>
    /// <returns>200 if revoked, 404 if merchant or key not found.</returns>
    [HttpDelete("{keyId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        Guid merchantId,
        Guid keyId,
        [FromServices] RevokeMerchantApiKeyHandler handler)
    {
        var result = await handler.Handle(new RevokeMerchantApiKeyCommand(merchantId, keyId));
        return result.ToActionResult();
    }
}
