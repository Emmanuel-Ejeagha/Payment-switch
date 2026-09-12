using BuildingBlocks.Shared.Email;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Tests.Services;

public class EmailSenderTests
{
    private sealed class CapturingLogger : ILogger<EmailSender>
    {
        public readonly List<string> Messages = new();

        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task SendAsync_WhenSmtpUnconfigured_DoesNotLogToken()
    {
        var logger = new CapturingLogger();
        var sender = new EmailSender(logger, Options.Create(new SmtpSettings()));
        var token = $"reset-token-{Guid.NewGuid():N}";

        var result = await sender.SendAsync(new EmailMessage(
            "user@example.com",
            "Reset your password",
            $"Use this link: https://app/reset?token={token}"));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(logger.Messages);
        Assert.DoesNotContain(logger.Messages, m => m.Contains(token));
    }
}
