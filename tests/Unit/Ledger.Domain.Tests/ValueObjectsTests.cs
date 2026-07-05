using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Tests;

public class ValueObjectsTests
{
    [Fact]
    public void Money_Valid_ShouldCreate()
    {
        var m = new Money(10m, "usd");
        Assert.Equal(10m, m.Amount);
        Assert.Equal("USD", m.Currency);
    }

    [Fact]
    public void Money_Negative_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(-1m, "USD"));
    }

    [Fact]
    public void Money_InvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(10m, "US"));
    }

    [Fact]
    public void Currency_Valid_ShouldCreate()
    {
        var c = new Currency("usd");
        Assert.Equal("USD", c.Code);
    }

    [Fact]
    public void Currency_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Currency("US"));
    }

    [Fact]
    public void CorrelationId_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CorrelationId(""));
    }

    [Fact]
    public void CorrelationId_Valid_ShouldSet()
    {
        var cid = new CorrelationId("test-id");
        Assert.Equal("test-id", cid.Value);
    }
}