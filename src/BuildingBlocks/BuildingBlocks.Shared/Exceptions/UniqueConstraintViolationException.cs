namespace BuildingBlocks.Shared.Exceptions;

/// <summary>
/// Thrown when a database uniqueness constraint rejects an insert/update.
/// </summary>
public class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException() : base("A unique constraint was violated.")
    {
    }

    public UniqueConstraintViolationException(string message) : base(message)
    {
    }
}
