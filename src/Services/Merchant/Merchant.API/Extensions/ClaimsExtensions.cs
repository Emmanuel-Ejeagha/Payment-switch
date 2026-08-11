using BuildingBlocks.Shared.Auth;
using Merchant.Application.Auth;
using System.Security.Claims;

namespace Merchant.API.Extensions;

public static class ClaimsExtensions
{
    public static CallerContext ToCallerContext(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdClaim, out var id) ? id : null;
        var email = user.FindFirstValue(ClaimTypes.Email);
        var isAdmin = user.IsInRole("Admin");
        var emailVerified = string.Equals(user.FindFirstValue(CustomClaimTypes.EmailVerified), "true", StringComparison.OrdinalIgnoreCase);

        return new CallerContext(userId, email, isAdmin, emailVerified);
    }
}
