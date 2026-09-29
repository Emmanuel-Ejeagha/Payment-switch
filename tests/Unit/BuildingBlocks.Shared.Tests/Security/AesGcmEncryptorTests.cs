using BuildingBlocks.Shared.Security;
using System.Security.Cryptography;

namespace BuildingBlocks.Shared.Tests.Security;

public class AesGcmEncryptorTests
{
    private static readonly string Secret = "a-long-strong-test-secret-at-least-32-chars!!";
    private static readonly AesGcmEncryptor Encryptor = AesGcmEncryptor.CreateFromSecret(Secret);

    [Fact]
    public void Encrypt_Decrypt_RoundTrips()
    {
        const string plaintext = "0123456789abcdef0123456789abcdef";

        var ciphertext = Encryptor.Encrypt(plaintext);
        var decrypted = Encryptor.Decrypt(ciphertext);

        Assert.Equal(plaintext, decrypted);
        Assert.NotEqual(plaintext, ciphertext);
    }

    [Fact]
    public void Encrypt_SamePlaintext_ProducesFreshNonceEachTime()
    {
        const string plaintext = "same-input";

        var first = Encryptor.Encrypt(plaintext);
        var second = Encryptor.Encrypt(plaintext);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var ciphertext = Encryptor.Encrypt("payload");
        var tampered = ciphertext[..^4] + (ciphertext[^4] == 'A' ? 'B' : 'A') + ciphertext[^3..];

        Assert.ThrowsAny<CryptographicException>(() => Encryptor.Decrypt(tampered));
    }

    [Fact]
    public void Decrypt_WrongKey_Throws()
    {
        var ciphertext = Encryptor.Encrypt("payload");
        var other = AesGcmEncryptor.CreateFromSecret("a-different-strong-secret-at-least-32-char!");

        Assert.ThrowsAny<CryptographicException>(() => other.Decrypt(ciphertext));
    }

    [Fact]
    public void Constructor_RejectsNon256BitKey()
    {
        Assert.Throws<ArgumentException>(() => new AesGcmEncryptor(new byte[16]));
    }
}
