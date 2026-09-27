using BuildingBlocks.Shared.Paging;

namespace BuildingBlocks.Shared.Tests;

public class PagingTests
{
    [Fact]
    public void Normalize_Defaults_AreUsedWhenNotProvided()
    {
        var (skip, take) = PageBounds.Normalize(PageBounds.DefaultSkip, PageBounds.DefaultTake);

        Assert.Equal(PageBounds.DefaultSkip, skip);
        Assert.Equal(PageBounds.DefaultTake, take);
    }

    [Fact]
    public void Normalize_NegativeSkip_ClampedToZero()
    {
        var (skip, take) = PageBounds.Normalize(-5, 20);

        Assert.Equal(0, skip);
        Assert.Equal(20, take);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Normalize_NonPositiveTake_ClampedToOne(int take)
    {
        var (_, normalizedTake) = PageBounds.Normalize(0, take);

        Assert.Equal(1, normalizedTake);
    }

    [Fact]
    public void Normalize_OversizedTake_ClampedToMaxTake()
    {
        var (_, take) = PageBounds.Normalize(0, 1_000_000);

        Assert.Equal(PageBounds.MaxTake, take);
    }

    [Fact]
    public void Normalize_MaxTake_PassesThrough()
    {
        var (_, take) = PageBounds.Normalize(10, PageBounds.MaxTake);

        Assert.Equal(PageBounds.MaxTake, take);
    }

    [Fact]
    public void Normalize_NegativeSkipWithOversizedTake_ClampsBoth()
    {
        var (skip, take) = PageBounds.Normalize(-1, 500);

        Assert.Equal(0, skip);
        Assert.Equal(PageBounds.MaxTake, take);
    }
}