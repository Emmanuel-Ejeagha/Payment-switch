using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Shared.Security;

/// <summary>
/// AES-256-GCM authenticated encryption with a fresh random nonce per message.
/// Output format: base64(nonce(12) || ciphertext || tag(16)). A 256-bit key is
/// derived from the configured secret via SHA-256, so any strong secret works
/// regardless of its encoding.
/// </summary>
public sealed class AesGcmEncryptor
{
    public const int NonceSize = 12;
    public const int TagSize = 16;
    public const int KeySize = 32;

    private readonly byte[] _key;

    public AesGcmEncryptor(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeySize)
            throw new ArgumentException("AES-GCM key must be 32 bytes (256-bit).", nameof(key));
        _key = key;
    }

    public static AesGcmEncryptor CreateFromSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return new AesGcmEncryptor(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var payload = new byte[nonce.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length + cipherBytes.Length, tag.Length);
        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ciphertext);
        var payload = Convert.FromBase64String(ciphertext);
        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Ciphertext is too short.");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[payload.Length - NonceSize - TagSize];
        Buffer.BlockCopy(payload, 0, nonce, 0, nonce.Length);
        Buffer.BlockCopy(payload, nonce.Length, cipherBytes, 0, cipherBytes.Length);
        Buffer.BlockCopy(payload, nonce.Length + cipherBytes.Length, tag, 0, tag.Length);

        var plainBytes = new byte[cipherBytes.Length];
        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
