using Microsoft.Extensions.Logging;
using Moq;
using Notification.Application.Features.Queries.GetNotificationPreferences;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Tests.Handlers;

public class GetNotificationPreferencesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRecipientPreferences()
    {
        var repoMock = new Mock<INotificationPreferenceRepository>();
        var loggerMock = new Mock<ILogger<GetNotificationPreferencesHandler>>();
        var preferences = new List<NotificationPreference>
        {
            new(Guid.NewGuid(), "merchant@acme.com", NotificationChannel.Email,
                Notification.Domain.NotificationEventTypes.PaymentAuthorized, false),
            new(Guid.NewGuid(), "merchant@acme.com", NotificationChannel.Email,
                Notification.Domain.NotificationEventTypes.PaymentCaptured, true)
        };
        repoMock.Setup(r => r.ListByRecipientAsync("merchant@acme.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        var handler = new GetNotificationPreferencesHandler(repoMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetNotificationPreferencesQuery(" Merchant@Acme.com "));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, p => Assert.Equal("merchant@acme.com", p.Recipient));
        Assert.Equal(Notification.Domain.NotificationEventTypes.PaymentAuthorized, result.Value[0].EventType);
        Assert.False(result.Value[0].Enabled);
    }
}
