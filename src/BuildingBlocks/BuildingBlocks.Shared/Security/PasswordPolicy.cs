namespace BuildingBlocks.Shared.Security;

/// <summary>
/// Single source of truth for user-facing password rules. Lives in the shared
/// kernel so every service validator enforces the identical policy, and the
/// frontends mirror it exactly (TASK-024).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 100;

    /// <summary>
    /// Notorious passwords rejected by exact (case-insensitive) match.
    /// Exact-match only: longer unique passphrases containing these fragments
    /// (e.g. "Test123456!") remain allowed.
    /// </summary>
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password1", "password12", "password123", "password1234", "password12345",
        "passw0rd", "passw0rd123",
        "qwerty", "qwerty1", "qwerty12", "qwerty123", "qwerty1234",
        "letmein", "letmein1", "letmein12", "letmein123",
        "welcome", "welcome1", "welcome12", "welcome123",
        "admin", "admin1", "admin12", "admin123", "admin1234",
        "abc123", "abc1234", "123456", "1234567", "12345678", "123456789",
        "iloveyou", "iloveyou1", "monkey123", "dragon123", "football1",
        "sunshine1", "master123", "superman1"
    };

    public static bool HasComplexity(string? password)
        => password is not null
           && password.Any(char.IsLetter)
           && password.Any(char.IsDigit)
           && (password.Any(char.IsUpper) || password.Any(c => !char.IsLetterOrDigit(c)));

    public static bool IsCommonPassword(string? password)
        => password is not null && CommonPasswords.Contains(password);

    public static bool IsValid(string? password)
        => password is not null
           && password.Length >= MinLength
           && password.Length <= MaxLength
           && HasComplexity(password)
           && !IsCommonPassword(password);
}