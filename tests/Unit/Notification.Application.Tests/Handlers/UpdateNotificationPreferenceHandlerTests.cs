using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Notification.Application.Features.Commands.UpdateNotificationPreference;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Tests.Handlers;

public class UpdateNotificationPreferenceHandlerTests
{
    private readonly Mock<INotificationPreferenceRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<UpdateNotificationPreferenceHandler>> _loggerMock = new();
    private readonly UpdateNotificationPreferenceHandler _handler;
    private readonly IValidator<UpdateNotificationPreferenceCommand> _validator = new UpdateNotificationPreferenceCommandValidator();

    public UpdateNotificationPreferenceHandlerTests()
    {
        _handler = new UpdateNotificationPreferenceHandler(_repoMock.Object, _uowMock.Object, _validator, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoExistingPreference_CreatesNewOne()
    {
        var command = ValidCommand(enabled: false);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repoMock.Verify(r => r.AddAsync(
            It.Is<NotificationPreference>(p =>
                p.Recipient == "merchant@acme.com" &&
                p.Channel.Value == "email" &&
                p.EventType == Notification.Domain.NotificationEventTypes.PaymentAuthorized &&
                !p.Enabled),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingPreference_UpdatesEnabled()
    {
        var command = ValidCommand(enabled: true);
        var existing = new NotificationPreference(
            Guid.NewGuid(), "merchant@acme.com", NotificationChannel.Email,
            Notification.Domain.NotificationEventTypes.PaymentAuthorized, false);
        _repoMock.Setup(r => r.FindAsync("merchant@acme.com", "email",
            Notification.Domain.NotificationEventTypes.PaymentAuthorized, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value);
        Assert.True(existing.Enabled);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidEventType_ReturnsValidationError()
    {
        var command = ValidCommand(enabled: true) with { EventType = "UnknownEvent" };

        var result = await _handler.Handle(command);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Event type", StringComparison.OrdinalIgnoreCase));
        _repoMock.Verify(r => r.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UpdateNotificationPreferenceCommand ValidCommand(bool enabled) =>
        new("Merchant@Acme.com", "email", Notification.Domain.NotificationEventTypes.PaymentAuthorized, enabled);
}
