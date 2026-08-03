using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Shared.Security;

public static class ApiKeyHasher
{
    private static string Normalize(string key) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    public static string Hash(string key) =>
        BCrypt.Net.BCrypt.HashPassword(Normalize(key));

    public static bool Verify(string key, string storedHash) =>
        BCrypt.Net.BCrypt.Verify(Normalize(key), storedHash);
}
