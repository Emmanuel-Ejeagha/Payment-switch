namespace Merchant.Application.Auth;

public record CallerContext(Guid? UserId, string? Email, bool IsAdmin, bool EmailVerified = false)
{
    public static CallerContext Anonymous => new(null, null, false);

    public bool CanAccess(Guid? ownerId)
        => IsAdmin || (UserId.HasValue && ownerId == UserId);

    public bool CanAccessMerchant(Guid? ownerId, string? merchantEmail)
        => CanAccess(ownerId)
           || (Email is not null && merchantEmail is not null
               && string.Equals(Email, merchantEmail, StringComparison.OrdinalIgnoreCase));
}
