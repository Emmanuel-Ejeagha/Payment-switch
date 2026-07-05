using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.Register;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<RegisterUserCommand>> _validatorMock = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldRegisterAndReturnUserId()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Password123", "John Doe");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.Hash(command.Password))
            .Returns(new PasswordHash("hashed_password"));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email.Value == command.Email), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterUserCommand("existing@example.com", "Password123", "Existing User");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Identity.EmailAlreadyInUse");
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        // Arrange
        var command = new RegisterUserCommand("", "", "");
        _validatorMock
        .Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ValidationResult(new[]
        {
            new ValidationFailure("Email", "A valid email address is required."),
            new ValidationFailure("Password", "Password must be at least 8 characters.")
        }));

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Email");
        Assert.Contains(result.Errors, e => e.Code == "Password");
    }

    private void SetupValidatorSuccess(RegisterUserCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(RegisterUserCommand command, string propertyName, string errorMessage)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }
}