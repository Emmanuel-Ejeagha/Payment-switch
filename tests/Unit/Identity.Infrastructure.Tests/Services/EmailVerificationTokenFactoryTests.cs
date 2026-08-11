using Identity.Infrastructure.Services;

namespace Identity.Infrastructure.Tests.Services;

public class EmailVerificationTokenFactoryTests
{
    private readonly EmailVerificationTokenFactory _factory = new();

    [Fact]
    public void Generate_ProducesDistinctPlainTextTokens()
    {
        var a = _factory.Generate(TimeSpan.FromHours(24));
        var b = _factory.Generate(TimeSpan.FromHours(24));

        Assert.NotEqual(a.PlainText, b.PlainText);
        Assert.NotEqual(a.Hash, b.Hash);
    }

    [Fact]
    public void Generate_StoresOnlyTheHash()
    {
        var token = _factory.Generate(TimeSpan.FromHours(24));

        Assert.NotEqual(token.PlainText, token.Hash);
        Assert.Equal(_factory.Hash(token.PlainText), token.Hash);
    }

    [Fact]
    public void Generate_ComputesExpiryFromLifetime()
    {
        var before = DateTime.UtcNow;
        var token = _factory.Generate(TimeSpan.FromHours(24));
        var after = DateTime.UtcNow.AddHours(24);

        Assert.InRange(token.ExpiresAtUtc, before.AddHours(24), after.AddHours(1));
    }

    [Fact]
    public void Hash_IsDeterministic()
    {
        Assert.Equal(_factory.Hash("token"), _factory.Hash("token"));
        Assert.NotEqual(_factory.Hash("token"), _factory.Hash("other"));
    }
}
