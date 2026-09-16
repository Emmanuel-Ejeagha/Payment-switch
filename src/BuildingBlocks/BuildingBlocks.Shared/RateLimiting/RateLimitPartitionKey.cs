using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BuildingBlocks.Shared.RateLimiting;

/// <summary>
/// Partition key for per-client rate limiting (Step 7.4). Authenticated
/// callers are bucketed by user id; anonymous callers by remote IP. A single
/// global bucket would let one client consume the budget for everyone (DoS)
/// and would not attribute brute-force traffic to its source.
/// NOTE: behind nginx the real client IP is restored by
/// UsePaymentSwitchForwardedHeaders, which runs before UseRateLimiter.
/// </summary>
public static class RateLimitPartitionKey
{
    public static string GetKey(HttpContext context)
    {
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
            return $"user:{userId}";

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{ip ?? "unknown"}";
    }
}
