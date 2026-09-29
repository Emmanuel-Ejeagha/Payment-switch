namespace BuildingBlocks.Shared.Paging;

/// <summary>
/// Shared list-API pagination conventions. All list endpoints accept
/// <c>skip</c>/<c>take</c> and normalize them with <see cref="Normalize"/> so
/// that clients can never request a negative offset or an unbounded page.
/// </summary>
public static class PageBounds
{
    public const int DefaultSkip = 0;
    public const int DefaultTake = 20;
    public const int MaxTake = 100;

    /// <summary>
    /// Clamps a client-supplied <paramref name="skip"/>/<paramref name="take"/>
    /// pair into safe bounds: <c>skip &gt;= 0</c>, <c>1 &lt;= take &lt;= MaxTake</c>.
    /// </summary>
    public static (int Skip, int Take) Normalize(int skip, int take)
    {
        var normalizedSkip = Math.Max(DefaultSkip, skip);
        var normalizedTake = Math.Clamp(take, 1, MaxTake);
        return (normalizedSkip, normalizedTake);
    }
}