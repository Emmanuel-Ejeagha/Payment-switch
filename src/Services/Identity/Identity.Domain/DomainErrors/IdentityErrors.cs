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

    public static Error InvalidCredentials =>
        new("Identity.InvalidCredentials", "Invalid email or password.");

    public static Error ApiKeyNotFound(Guid keyId) =>
        new("Identity.ApiKeyNotFound", $"API key with Id '{keyId}' not found.");
}