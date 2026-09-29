using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Shared.Email;

/// <summary>
/// Renders an email body safe for logs. Simulated-email paths must never log
/// message bodies: verification and password-reset tokens live there and would
/// leak into log aggregators. A truncated SHA-256 keeps entries correlatable
/// for debugging without exposing secrets.
/// </summary>
public static class EmailLogRedactor
{
    public static string Redact(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return "(empty)";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()[..16]}";
    }
}
