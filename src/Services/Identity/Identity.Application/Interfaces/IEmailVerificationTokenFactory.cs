namespace Identity.Application.Interfaces;

public record EmailVerificationTokenData(string PlainText, string Hash, DateTime ExpiresAtUtc);

/// <summary>
/// Produces and hashes CSPRNG email-verification tokens. The plain token is
/// returned exactly once (embedded in the email link); only the hash is stored.
/// </summary>
public interface IEmailVerificationTokenFactory
{
    EmailVerificationTokenData Generate(TimeSpan lifetime);

    string Hash(string token);
}
