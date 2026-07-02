using Moq;
using Notification.Application.Features.Queries.GetNotificationById;
using Notification.Application.Interfaces;
using Notification.Domain.ValueObjects;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Application.Tests.Handlers.Queries;

public class GetNotificationByIdHandlerTests
{
    [Fact]
    public async Task Handle_Found_ReturnsDto()
    {
        var notification = new NotificationEntity(Guid.NewGuid(), "test@test.com", NotificationChannel.Email, "Sub", "Bod", null, "{}");
        var repoMock = new Mock<INotificationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);
        var handler = new GetNotificationByIdHandler(repoMock.Object);

        var result = await handler.Handle(new GetNotificationByIdQuery(notification.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(notification.Recipient, result.Value.Recipient);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        var repoMock = new Mock<INotificationRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificationEntity?)null);
        var handler = new GetNotificationByIdHandler(repoMock.Object);

        var result = await handler.Handle(new GetNotificationByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Notification.NotFound", result.Errors[0].Code);
    }
}