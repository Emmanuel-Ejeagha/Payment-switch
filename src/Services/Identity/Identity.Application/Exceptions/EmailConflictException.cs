namespace Identity.Application.Exceptions;

/// <summary>
/// Thrown by the persistence layer when a save would violate the unique email
/// index. Acts as a backstop for the application-level existence check so a
/// concurrent or case-variant duplicate never surfaces as a generic 500.
/// </summary>
public sealed class EmailConflictException : Exception
{
    public EmailConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
