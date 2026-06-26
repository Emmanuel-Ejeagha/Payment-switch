using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.ApiKey;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class RevokeApiKeyHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<RevokeApiKeyCommand>> _validatorMock = new();
    private readonly RevokeApiKeyHandler _handler;

    public RevokeApiKeyHandlerTests()
    {
        _handler = new RevokeApiKeyHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_ValidKey_ShouldRevoke()
    {
        // Arrange
        var user = CreateUserWithApiKey(out var keyId);
        var command = new RevokeApiKeyCommand(user.Id, keyId);
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(user.ApiKeys[0].RevokedAt);
    }

    [Fact]
    public async Task Handle_ApiKeyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUserWithApiKey(out _);
        var command = new RevokeApiKeyCommand(user.Id, Guid.NewGuid());
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.ApiKeyNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new RevokeApiKeyCommand(Guid.NewGuid(), Guid.NewGuid());
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.UserNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        // Arrange
        var command = new RevokeApiKeyCommand(Guid.Empty, Guid.Empty);
        SetupValidatorFailure(command, "UserId", "'User Id' must not be empty.");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "UserId");
    }

    private static User CreateUserWithApiKey(out Guid keyId)
    {
        var user = new User(Guid.NewGuid(), new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));
        keyId = Guid.NewGuid();
        user.GenerateApiKey(keyId, "hashed_key", "test");
        user.ClearDomainEvents();
        return user;
    }

    private void SetupValidatorSuccess(RevokeApiKeyCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(RevokeApiKeyCommand command, string propertyName, string errorMessage)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }
}