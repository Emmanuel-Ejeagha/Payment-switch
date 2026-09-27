namespace BuildingBlocks.Shared.Exceptions;

/// <summary>
/// Thrown when a concurrent update is detected (optimistic concurrency conflict).
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() : base("A concurrency conflict occurred.")
    {
    }

    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}
