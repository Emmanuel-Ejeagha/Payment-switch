using BuildingBlocks.Shared.Results;

namespace Identity.Domain.DomainErrors;

public static class IdentityErrors
{
    public static Error EmailAlreadyInUse(string email) =>
        new("Identity.EmailAlreadyInUse", $"Email '{email}' is already registered.");

    public static Error InvalidEmailFormat(string email) =>
        new("Identity.InvalidEmailFormat", $"Email '{email}' has an invalid format.");

    public static Error UserNotFound(Guid userId) =>
        new("Identity.UserNotFound", $"User with Id '{userId}' not found.");

    public static Error UserNotFoundByEmail(string email) =>
        new("Identity.UserNotFound", $"User with email '{email}' not found.");

    public static Error InvalidCredentials =>
        new("Identity.InvalidCredentials", "Invalid email or password.");

    public static Error AccountLocked =>
        new("Identity.AccountLocked", "Too many failed sign-in attempts. Please try again later.");

    public static Error EmailNotVerified =>
        new("Identity.EmailNotVerified", "Please verify your email address before proceeding.");

    public static Error EmailAlreadyVerified =>
        new("Identity.EmailAlreadyVerified", "This email address is already verified.");

    public static Error InvalidVerificationToken =>
        new("Identity.InvalidVerificationToken", "This verification link is invalid or has already been used.");

    public static Error VerificationTokenExpired =>
        new("Identity.VerificationTokenExpired", "This verification link has expired. Request a new one.");

    public static Error InvalidCurrentPassword =>
        new("Identity.InvalidCurrentPassword", "The current password is incorrect.");

    public static Error InvalidPasswordResetToken =>
        new("Identity.InvalidPasswordResetToken", "This password reset link is invalid or has already been used.");

    public static Error PasswordResetTokenExpired =>
        new("Identity.PasswordResetTokenExpired", "This password reset link has expired. Request a new one.");

    public static Error ApiKeyNotFound(Guid keyId) =>
        new("Identity.ApiKeyNotFound", $"API key with Id '{keyId}' not found.");
}