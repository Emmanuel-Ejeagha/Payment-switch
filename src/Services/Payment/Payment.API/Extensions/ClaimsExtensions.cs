using Payment.Application.Auth;
using System.Security.Claims;

namespace Payment.API.Extensions;

public static class ClaimsExtensions
{
    public static CallerContext ToCallerContext(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdClaim, out var id) ? id : null;
        var email = user.FindFirstValue(ClaimTypes.Email);
        var isAdmin = user.IsInRole("Admin");

        return new CallerContext(userId, email, isAdmin);
    }
}
