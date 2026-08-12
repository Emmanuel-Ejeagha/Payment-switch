namespace BuildingBlocks.Shared.Security;

/// <summary>
/// Single source of truth for user-facing password rules. Lives in the shared
/// kernel so every service validator enforces the identical policy, and the
/// frontends mirror it exactly (TASK-024).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 100;

    public static bool IsValid(string? password)
        => password is not null
           && password.Length >= MinLength
           && password.Length <= MaxLength
           && password.Any(char.IsLetter)
           && password.Any(char.IsDigit);
}