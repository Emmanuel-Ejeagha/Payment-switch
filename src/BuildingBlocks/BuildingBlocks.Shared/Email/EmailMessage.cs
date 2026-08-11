namespace BuildingBlocks.Shared.Email;

/// <summary>
/// A self-contained email payload decoupled from any transport.
/// </summary>
public sealed record EmailMessage(
    string To,
    string Subject,
    string TextBody,
    string? HtmlBody = null);
