using BuildingBlocks.Shared.Security;

namespace Merchant.Infrastructure.Security;

/// <summary>
/// Encrypts/decrypts webhook secrets for persistence (TASK-006). Encrypted
/// values are prefixed with <c>enc:</c> so legacy plaintext rows stay readable
/// until the one-time backfill re-encrypts them.
/// </summary>
public static class WebhookSecretEncryptor
{
    public const string Prefix = "enc:";

    private static AesGcmEncryptor? _encryptor;

    public static void Initialize(string secret)
    {
        _encryptor = AesGcmEncryptor.CreateFromSecret(secret);
    }

    public static string Encrypt(string plaintext)
    {
        return Prefix + GetEncryptor().Encrypt(plaintext);
    }

    public static string Decrypt(string value)
    {
        if (IsEncrypted(value))
            return GetEncryptor().Decrypt(value[Prefix.Length..]);
        return value;
    }

    public static bool IsEncrypted(string value) =>
        value.StartsWith(Prefix, StringComparison.Ordinal);

    private static AesGcmEncryptor GetEncryptor() =>
        _encryptor ?? throw new InvalidOperationException(
            "WebhookSecretEncryptor is not initialized. Set WebhookSecretEncryption:Key.");
}
