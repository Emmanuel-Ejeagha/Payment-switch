namespace Identity.Application.Configuration;

public class EmailVerificationOptions
{
    /// <summary>Base URL of the frontend that hosts the /verify-email page.</summary>
    public string FrontendBaseUrl { get; set; } = "";

    public int TokenLifetimeHours { get; set; } = 24;

    public string Subject { get; set; } = "Confirm your email address";
}
