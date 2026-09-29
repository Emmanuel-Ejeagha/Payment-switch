using Asp.Versioning;
using BuildingBlocks.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Middlewares;

namespace Payment.API.Controllers;

/// <summary>
/// Service-to-service cache maintenance (Step 7.4). Merchant calls this
/// best-effort after revoking an API key so the cached resolution stops being
/// honored immediately instead of lingering for the cache TTL. Gated by the
/// ServiceOnly policy (short-lived service JWT), reachable only on the
/// internal network; a missed notify is backstopped by the short TTL.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthPolicies.ServiceOnly)]
[Route("api/v{version:apiVersion}/internal/apikeys")]
[Produces("application/json")]
public class InternalApiKeysController : ControllerBase
{
    private readonly ApiKeyResolutionCache _cache;
    private readonly ILogger<InternalApiKeysController> _logger;

    public InternalApiKeysController(ApiKeyResolutionCache cache, ILogger<InternalApiKeysController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public sealed record PurgeApiKeyCacheRequest(Guid MerchantId);

    /// <summary>Drop every cached API-key resolution for a merchant.</summary>
    [HttpPost("purge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Purge([FromBody] PurgeApiKeyCacheRequest request)
    {
        var removed = _cache.PurgeByMerchant(request.MerchantId);
        _logger.LogInformation("Purged {Count} cached API-key resolutions for merchant {MerchantId}", removed, request.MerchantId);
        return Ok(new { purged = removed });
    }
}
