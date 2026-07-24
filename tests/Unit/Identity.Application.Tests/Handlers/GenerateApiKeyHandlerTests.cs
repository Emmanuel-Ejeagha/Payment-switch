using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.ApiKey;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class GenerateApiKeyHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<GenerateApiKeyCommand>> _validatorMock = new();
    private readonly Mock<ILogger<GenerateApiKeyHandler>> _loggerMock = new();
    private readonly GenerateApiKeyHandler _handler;

    public GenerateApiKeyHandlerTests()
    {
        _handler = new GenerateApiKeyHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUser_ShouldCreateApiKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new GenerateApiKeyCommand(userId, "live");
        var user = CreateUser(userId);
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("live", result.Value.Environment);
        Assert.NotEmpty(result.Value.PlainTextKey);
        Assert.Single(user.ApiKeys);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new GenerateApiKeyCommand(userId, "test");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.UserNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidEnvironment_ShouldReturnValidationErrors()
    {
        // Arrange
        var command = new GenerateApiKeyCommand(Guid.NewGuid(), "invalid");
        SetupValidatorFailure(command, "Environment", "Environment must be 'live' or 'test'.");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Environment");
    }

    private static User CreateUser(Guid userId)
        => new(userId, new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));

    private void SetupValidatorSuccess(GenerateApiKeyCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(GenerateApiKeyCommand command, string propertyName, string errorMessage)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }
}