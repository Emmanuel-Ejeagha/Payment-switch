namespace Identity.Application.Configuration;

public class EmailVerificationOptions
{
    /// <summary>Base URL of the frontend that hosts the /verify-email page.</summary>
    public string FrontendBaseUrl { get; set; } = "";

    public int TokenLifetimeHours { get; set; } = 24;

    /// <summary>
    /// Minimum seconds between verification emails for the same address.
    /// Enforced in the database-backed handler so it holds across instances.
    /// </summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    public string Subject { get; set; } = "Confirm your email address";
}
