using BuildingBlocks.Shared.Email;

namespace Identity.Application.Services;

/// <summary>
/// Builds the password-reset email, embedding the single-use token in a link to
/// the frontend /reset-password page. When no frontend base URL is configured the
/// token is embedded in the body so the flow still works in development.
/// </summary>
public static class ResetPasswordEmailBuilder
{
    public static EmailMessage Build(
        string recipient,
        string plainToken,
        string subject,
        string frontendBaseUrl)
    {
        var baseUrl = frontendBaseUrl.TrimEnd('/');
        var email = Uri.EscapeDataString(recipient);

        string textBody;
        string? htmlBody = null;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            textBody = "Reset your password.\n\n" +
                       $"Your password reset token is: {plainToken}\n\n" +
                       "It expires in 1 hour. If you did not request this, you can safely ignore this email.";
        }
        else
        {
            var link = $"{baseUrl}/reset-password?email={email}&token={Uri.EscapeDataString(plainToken)}";
            textBody = "Reset your password.\n\n" +
                       $"Open the link below to set a new password:\n{link}\n\n" +
                       "If the link does not open, copy and paste it into your browser. It expires in 1 hour.";
            htmlBody = $"<h2>Reset your password</h2>" +
                       $"<p>Hi,</p>" +
                       $"<p>We received a request to reset your password. Click the button below to choose a new one:</p>" +
                       $"<p style=\"margin:24px 0\"><a href=\"{link}\" style=\"background:#111827;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none\">Reset password</a></p>" +
                       $"<p>Or copy and paste this link into your browser:</p>" +
                       $"<p style=\"word-break:break-all\"><a href=\"{link}\">{link}</a></p>" +
                       $"<p style=\"color:#6b7280;font-size:12px\">This link expires in 1 hour.</p>" +
                       $"<p style=\"color:#6b7280;font-size:12px\">If you did not request this, you can safely ignore this email.</p>";
        }

        return new EmailMessage(recipient, subject, textBody, htmlBody);
    }
}
