using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Notification.Application.Configuration;
using Notification.Domain.ValueObjects;
using Notification.Infrastructure.Senders;
using Resend;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Tests.Senders;

public class ResendEmailSenderTests
{
    private readonly Mock<IResend> _resendMock = new();
    private readonly ResendSettings _settings = new()
    {
        ApiKey = "re_testkey",
        FromEmail = "noreply@paymentswitch.com",
        FromName = "PaymentSwitch",
        TimeoutSeconds = 30
    };
    private readonly VerificationEmailOptions _linkOptions = new()
    {
        FrontendBaseUrl = "https://merchant.example.com",
        Subject = "Confirm your email address"
    };
    private readonly Mock<IHostEnvironment> _environmentMock = new();

    public ResendEmailSenderTests()
    {
        _environmentMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
    }

    private ResendEmailSender CreateSender() => new(
        NullLogger<ResendEmailSender>.Instance,
        Options.Create(_settings),
        _resendMock.Object,
        Options.Create(_linkOptions),
        _environmentMock.Object);

    private static NotificationEntity VerificationNotification() => new(
        Guid.NewGuid(),
        "user@example.com",
        NotificationChannel.FromString("email"),
        "Confirm your email address",
        "body",
        null,
        """{"UserId":"11111111-1111-1111-1111-111111111111","Email":"user@example.com","Token":"ABCDEF","IssuedAtUtc":"2026-09-30T10:00:00Z","ExpiresAtUtc":"2026-10-01T10:00:00Z"}""",
        5,
        "resend");

    private static ResendResponse<Guid> OkResponse(Guid id) =>
        new(id, new ResendRateLimit());

    private static ResendResponse<Guid> FailedResponse(HttpStatusCode statusCode, ErrorType errorType) =>
        new(new ResendException(statusCode, errorType, "provider refused", new ResendRateLimit()), new ResendRateLimit());

    [Fact]
    public async Task SendAsync_Success_LogsProviderId()
    {
        var id = Guid.NewGuid();
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OkResponse(id));

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsSuccess);
        _resendMock.Verify(r => r.EmailSendAsync(
            It.Is<EmailMessage>(m => m.Subject == "Confirm your email address" && m.HtmlBody.Contains("verify-email")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_TransientProviderError_MapsToTransientFailure()
    {
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailedResponse(HttpStatusCode.TooManyRequests, ErrorType.RateLimitExceeded));

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsFailure);
        Assert.Equal("Email.TransientFailure", result.Errors[0].Code);
    }

    [Fact]
    public void ResendSdk_MarksOnlyRateLimitAndTransportAsTransient()
    {
        // Pins the provider's own retry contract (verified against Resend 0.19.0):
        // a structured 5xx error is NOT transient there — it rides the bounded
        // retry budget like any permanent rejection instead of looping forever.
        Assert.True(new ResendException(HttpStatusCode.TooManyRequests, ErrorType.RateLimitExceeded, "x", new ResendRateLimit()).IsTransient);
        Assert.True(new ResendException(null, ErrorType.HttpSendFailed, "x", new ResendRateLimit()).IsTransient);
        Assert.False(new ResendException(HttpStatusCode.InternalServerError, ErrorType.InternalServerError, "x", new ResendRateLimit()).IsTransient);
        Assert.False(new ResendException(HttpStatusCode.BadRequest, ErrorType.ValidationError, "x", new ResendRateLimit()).IsTransient);
    }

    [Fact]
    public async Task SendAsync_StructuredServerError_MapsToProviderRejected()
    {
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailedResponse(HttpStatusCode.InternalServerError, ErrorType.InternalServerError));

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsFailure);
        Assert.Equal("Email.ProviderRejected", result.Errors[0].Code);
    }

    [Fact]
    public async Task SendAsync_PermanentProviderError_MapsToProviderRejected()
    {
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailedResponse(HttpStatusCode.BadRequest, ErrorType.ValidationError));

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsFailure);
        Assert.Equal("Email.ProviderRejected", result.Errors[0].Code);
    }

    [Fact]
    public async Task SendAsync_ThrownTransient_MapsToTransientFailure()
    {
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResendException(null, ErrorType.HttpSendFailed, "connection reset", new ResendRateLimit()));

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsFailure);
        Assert.Equal("Email.TransientFailure", result.Errors[0].Code);
    }

    [Fact]
    public async Task SendAsync_UnexpectedThrow_MapsToSendFailed()
    {
        // Provider outage with no SDK envelope: must surface as failure,
        // never as a false success.
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsFailure);
        Assert.Equal("Email.SendFailed", result.Errors[0].Code);
    }

    [Fact]
    public async Task SendAsync_OwnTimeout_MapsToTransientFailure()
    {
        _settings.TimeoutSeconds = 1;
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async (EmailMessage _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
                return OkResponse(Guid.NewGuid());
            });

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsFailure);
        Assert.Equal("Email.TransientFailure", result.Errors[0].Code);
    }

    [Fact]
    public async Task SendAsync_CallerCancelled_Rethrows()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _resendMock.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns((EmailMessage _, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new ResendResponse<Guid>(Guid.NewGuid(), new ResendRateLimit()));
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateSender().SendAsync(VerificationNotification(), cts.Token));
    }

    [Fact]
    public async Task SendAsync_MalformedPayload_FailsWithoutCallingProvider()
    {
        var notification = new NotificationEntity(
            Guid.NewGuid(), "user@example.com", NotificationChannel.FromString("email"),
            "Subject", "body", null, "not-json", 5, "resend");

        var result = await CreateSender().SendAsync(notification);

        Assert.True(result.IsFailure);
        Assert.Equal("Email.InvalidPayload", result.Errors[0].Code);
        _resendMock.Verify(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_UnconfiguredInDevelopment_SimulatesWithoutToken()
    {
        _settings.ApiKey = "";
        _environmentMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsSuccess);
        _resendMock.Verify(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_UnconfiguredInProduction_FailsLoudly()
    {
        _settings.ApiKey = "";

        var result = await CreateSender().SendAsync(VerificationNotification());

        Assert.True(result.IsFailure);
        Assert.Equal("Email.NotConfigured", result.Errors[0].Code);
        _resendMock.Verify(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
