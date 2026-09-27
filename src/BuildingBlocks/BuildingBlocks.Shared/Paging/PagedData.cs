namespace BuildingBlocks.Shared.Paging;

/// <summary>
/// Internal carrier for a paginated query: the page <paramref name="Items"/>
/// plus the <paramref name="TotalCount"/> matching the filter (before paging).
/// Controllers map this to a bare array body and expose the count via the
/// <c>X-Total-Count</c> response header.
/// </summary>
public record PagedData<T>(IReadOnlyList<T> Items, int TotalCount);