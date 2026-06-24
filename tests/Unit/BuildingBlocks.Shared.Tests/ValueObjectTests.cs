namespace BuildingBlocks.Shared.Tests;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

public class ValueObjectTests
{
    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        Assert.True(money1.Equals(money2));
        Assert.True(money1 == money2);
    }

    [Fact]
    public void Equals_DifferentValues_ShouldNotBeEqual()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(200m, "USD");

        Assert.False(money1.Equals(money2));
        Assert.True(money1 != money2);
    }

    [Fact]
    public void Equals_DifferentCurrency_ShouldNotBeEqual()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "EUR");

        Assert.False(money1.Equals(money2));
    }

    [Fact]
    public void Equals_Null_ShouldReturnFalse()
    {
        var money = new Money(100m, "USD");

        Assert.False(money.Equals(null));
        Assert.False(money == null);
    }

    [Fact]
    public void Equals_DifferentType_ShouldReturnFalse()
    {
        var money = new Money(100m, "USD");
        var other = new object();

        Assert.False(money.Equals(other));
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldMatch()
    {
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        Assert.Equal(money1.GetHashCode(), money2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_NullBothSides_ShouldBeTrue()
    {
        Money? left = null;
        Money? right = null;

        Assert.True(left == right);
    }

    [Fact]
    public void OperatorEquals_OneSideNull_ShouldBeFalse()
    {
        var left = new Money(100m, "USD");
        Money? right = null;

        Assert.False(left == right);
    }
}