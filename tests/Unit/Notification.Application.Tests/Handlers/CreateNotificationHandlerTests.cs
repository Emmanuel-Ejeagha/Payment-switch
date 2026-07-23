using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Notification.Application.Features.Commands.CreateNotification;
using Notification.Application.Interfaces;
using NotificationEntity =  Notification.Domain.Entities.Notification;

namespace Notification.Application.Tests.Handlers;

public class CreateNotificationHandlerTests
{
    private readonly Mock<INotificationRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<CreateNotificationCommand>> _validatorMock = new();
    private readonly Mock<ILogger<CreateNotificationHandler>> _loggerMock = new();
    private readonly CreateNotificationHandler _handler;

    public CreateNotificationHandlerTests()
    {
        _handler = new CreateNotificationHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateNotification()
    {
        var command = new CreateNotificationCommand("user@test.com", "email", "Hello", "World", null, "{}");
        SetupValidatorSuccess(command);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repoMock.Verify(r => r.AddAsync(It.Is<NotificationEntity>(n => n.Recipient == command.Recipient), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        var command = new CreateNotificationCommand("", "", null, null, null, "");
        SetupValidatorFailure(command, "Recipient", "Recipient is required.");

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Recipient");
    }

    private void SetupValidatorSuccess(CreateNotificationCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupValidatorFailure(CreateNotificationCommand command, string property, string error) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, error) }));
}