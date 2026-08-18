using Moq;
using Notification.Application.DTOs;
using Notification.Application.Features.Queries.ListNotifications;
using Notification.Application.Interfaces;

namespace Notification.Application.Tests.Handlers.Queries;

public class ListNotificationsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsList()
    {
        var dtos = new List<NotificationDto> { new(Guid.NewGuid(), "a@b.com", "email", "s", "b", null, "pending", 0, null, DateTime.UtcNow, null) };
        var repoMock = new Mock<INotificationRepository>();
        repoMock.Setup(r => r.ListAsync(null, null, null, 0, 10, It.IsAny<CancellationToken>())).ReturnsAsync(dtos);
        repoMock.Setup(r => r.CountAsync(null, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var loggerMock = new Mock<ILogger<ListNotificationsHandler>>();
        var handler = new ListNotificationsHandler(repoMock.Object, loggerMock.Object);

        var result = await handler.Handle(new ListNotificationsQuery(null, null, null, 0, 10));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value!.TotalCount);
    }
}
