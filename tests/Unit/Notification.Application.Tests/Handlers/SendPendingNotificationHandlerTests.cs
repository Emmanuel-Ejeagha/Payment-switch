using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using Moq;
using Notification.Application.Features.Commands.SendPendingNotification;
using Notification.Application.Interfaces;
using Notification.Domain.ValueObjects;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Application.Tests.Handlers;

public class SendPendingNotificationHandlerTests
{
    private readonly Mock<INotificationRepository> _repoMock = new();
    private readonly Mock<INotificationSender> _senderMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<ILogger<SendPendingNotificationHandler>> _loggerMock = new();
    private readonly SendPendingNotificationHandler _handler;

    public SendPendingNotificationHandlerTests()
    {
        _handler = new SendPendingNotificationHandler(_repoMock.Object, _senderMock.Object, _uowMock.Object, _dispatcherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_SendSuccess_ShouldMarkAsSent()
    {
        var notification = CreatePendingNotification();
        var command = new SendPendingNotificationCommand(notification.Id);
        _repoMock.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);
        _senderMock.Setup(s => s.SendAsync(notification, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
    }

    [Fact]
    public async Task Handle_SendFails_ShouldMarkAsFailed()
    {
        var notification = CreatePendingNotification();
        var command = new SendPendingNotificationCommand(notification.Id);
        _repoMock.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);
        _senderMock.Setup(s => s.SendAsync(notification, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure(new Error("Send.Error", "Failed")));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess); // handler still returns success, but notification status updated
        Assert.Equal(1, notification.RetryCount);
        Assert.NotNull(notification.NextRetryAt);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        var command = new SendPendingNotificationCommand(Guid.NewGuid());
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificationEntity?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.NotFound", result.Errors[0].Code);
    }

    private NotificationEntity CreatePendingNotification() =>
        new(Guid.NewGuid(), "test@test.com", NotificationChannel.Email, "Sub", "Bod", null, "{}");
}