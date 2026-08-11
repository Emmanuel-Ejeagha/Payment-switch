using Identity.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Services;

/// <summary>
/// CSPRNG verification tokens. Only the SHA-256 hash is ever stored.
/// </summary>
public class EmailVerificationTokenFactory : IEmailVerificationTokenFactory
{
    public EmailVerificationTokenData Generate(TimeSpan lifetime)
    {
        var plainText = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        return new EmailVerificationTokenData(plainText, Hash(plainText), DateTime.UtcNow.Add(lifetime));
    }

    public string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
