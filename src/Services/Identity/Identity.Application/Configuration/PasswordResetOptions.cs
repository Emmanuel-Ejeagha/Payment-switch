namespace Identity.Application.Configuration;

public class PasswordResetOptions
{
    /// <summary>Base URL of the frontend that hosts the /reset-password page.</summary>
    public string FrontendBaseUrl { get; set; } = "";

    public int TokenLifetimeHours { get; set; } = 1;

    public string Subject { get; set; } = "Reset your password";
}
