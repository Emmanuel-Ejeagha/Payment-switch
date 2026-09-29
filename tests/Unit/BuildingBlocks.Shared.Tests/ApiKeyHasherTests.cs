using BuildingBlocks.Shared.Security;

namespace BuildingBlocks.Shared.Tests;

public class ApiKeyHasherTests
{
    [Fact]
    public void Hash_ShouldProduceUniqueSaltedHashes_ForSameKey()
    {
        var first = ApiKeyHasher.Hash("sk_test_abc123");
        var second = ApiKeyHasher.Hash("sk_test_abc123");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForMatchingKey()
    {
        var key = "sk_test_abc123";
        var hash = ApiKeyHasher.Hash(key);

        Assert.True(ApiKeyHasher.Verify(key, hash));
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForDifferentKey()
    {
        var hash = ApiKeyHasher.Hash("sk_test_abc123");

        Assert.False(ApiKeyHasher.Verify("sk_test_def456", hash));
    }
}
