using BuildingBlocks.Shared.Results;

namespace BuildingBlocks.Shared.Email;

/// <summary>
/// Sends transactional emails. Implementations must not throw for expected
/// delivery failures; they return a Result so callers can decide the impact.
/// When SMTP is not configured, a non-silent log must be emitted instead of a
/// silent success.
/// </summary>
public interface IEmailSender
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
