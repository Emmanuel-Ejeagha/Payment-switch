using BuildingBlocks.Shared.Email;
using BuildingBlocks.Shared.Results;

namespace Identity.API.IntegrationTests;

/// <summary>
/// Test double that records outbound email instead of sending it, exposing the
/// single-use plaintext tokens embedded in the bodies for the E2E flow.
/// </summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly List<EmailMessage> _sent = new();
    private readonly object _sync = new();

    public IReadOnlyList<EmailMessage> Sent
    {
        get { lock (_sync) return _sent.ToList(); }
    }

    public Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        lock (_sync) _sent.Add(message);
        return Task.FromResult(Result.Success());
    }

    public void Reset()
    {
        lock (_sync) _sent.Clear();
    }
}