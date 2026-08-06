using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.Features.Command.ReplayWebhookEvent;
using Payment.Application.Features.Command.SendTestWebhookEvent;
using Payment.Application.Features.Queries.ListWebhookEvents;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Tests.Handlers;

public class SendTestWebhookEventHandlerTests
{
    private readonly Mock<IWebhookEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<SendTestWebhookEventCommand>> _validatorMock = new();
    private readonly Mock<ILogger<SendTestWebhookEventHandler>> _loggerMock = new();
    private readonly SendTestWebhookEventHandler _handler;

    public SendTestWebhookEventHandlerTests()
    {
        _handler = new SendTestWebhookEventHandler(
            _repoMock.Object,
            _uowMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldEnqueuePendingTestEvent()
    {
        var command = new SendTestWebhookEventCommand(Guid.NewGuid(), "test.event");
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("test.event", result.Value!.EventType);
        Assert.Equal(WebhookEvent.StatusPending, result.Value!.Status);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<WebhookEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldFail()
    {
        var command = new SendTestWebhookEventCommand(Guid.NewGuid(), "");
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("EventType", "Required") }));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<WebhookEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class ReplayWebhookEventHandlerTests
{
    private readonly Mock<IWebhookEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<ReplayWebhookEventCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ReplayWebhookEventHandler>> _loggerMock = new();
    private readonly ReplayWebhookEventHandler _handler;

    public ReplayWebhookEventHandlerTests()
    {
        _handler = new ReplayWebhookEventHandler(
            _repoMock.Object,
            _uowMock.Object,
            _dispatcherMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_FailedEvent_ShouldResetToPending()
    {
        var merchantId = Guid.NewGuid();
        var webhookEvent = CreateFailedEvent(merchantId);
        var command = new ReplayWebhookEventCommand(merchantId, webhookEvent.Id);
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        _repoMock.Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhookEvent);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(WebhookEvent.StatusPending, webhookEvent.Status);
        Assert.Equal(0, webhookEvent.Attempts);
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldFail()
    {
        var command = new ReplayWebhookEventCommand(Guid.NewGuid(), Guid.NewGuid());
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((WebhookEvent?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Webhook.EventNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_EventOfAnotherMerchant_ShouldFail()
    {
        var webhookEvent = CreateFailedEvent(Guid.NewGuid());
        var command = new ReplayWebhookEventCommand(Guid.NewGuid(), webhookEvent.Id);
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
        _repoMock.Setup(r => r.GetByIdAsync(webhookEvent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(webhookEvent);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Webhook.EventNotFound", result.Errors[0].Code);
    }

    private static WebhookEvent CreateFailedEvent(Guid merchantId)
    {
        var webhookEvent = new WebhookEvent(Guid.NewGuid(), merchantId, "PaymentAuthorizedDomainEvent", "{}");
        webhookEvent.MarkFailed("timeout", TimeSpan.FromSeconds(30));
        return webhookEvent;
    }
}

public class ListWebhookEventsHandlerTests
{
    private readonly Mock<IWebhookEventRepository> _repoMock = new();
    private readonly Mock<ILogger<ListWebhookEventsHandler>> _loggerMock = new();
    private readonly ListWebhookEventsHandler _handler;

    public ListWebhookEventsHandlerTests()
    {
        _handler = new ListWebhookEventsHandler(_repoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtos()
    {
        var merchantId = Guid.NewGuid();
        var webhookEvent = new WebhookEvent(Guid.NewGuid(), merchantId, "PaymentCapturedDomainEvent", "{}");
        webhookEvent.MarkSucceeded();
        _repoMock.Setup(r => r.ListByMerchantAsync(merchantId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebhookEvent> { webhookEvent });

        var result = await _handler.Handle(new ListWebhookEventsQuery(merchantId));

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value!);
        Assert.Equal(webhookEvent.Id, dto.Id);
        Assert.Equal(WebhookEvent.StatusSucceeded, dto.Status);
    }
}
