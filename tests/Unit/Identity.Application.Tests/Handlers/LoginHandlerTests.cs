using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.Login;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<LoginCommand>> _validatorMock = new();
    private readonly Mock<ILogger<LoginHandler>> _loggerMock = new();
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _handler = new LoginHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "Password123");
        var user = CreateActiveUser("user@example.com");

        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(true);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user))
            .Returns("access_token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh_token");
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("access_token", result.Value.AccessToken);
        Assert.Equal("refresh_token", result.Value.RefreshToken);
        Assert.Equal(3600, result.Value.ExpiresIn);
        Assert.Single(user.RefreshTokens);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand("unknown@example.com", "Password123");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidCredentials", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "WrongPassword");
        var user = CreateActiveUser("user@example.com");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidCredentials", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new LoginCommand("inactive@example.com", "Password123");
        var user = CreateInactiveUser("inactive@example.com");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.UserInactive", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        // Arrange
        var command = new LoginCommand("", "");
        SetupValidatorFailure(command, "Email", "'Email' must not be empty.");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Email");
    }

    private static User CreateActiveUser(string email)
    {
        var user = new User(Guid.NewGuid(), new Email(email), new PasswordHash("hashed"), new FullName("Test User"));
        user.ClearDomainEvents();
        return user;
    }

    private static User CreateInactiveUser(string email)
    {
        var user = CreateActiveUser(email);
        user.Deactivate();
        user.ClearDomainEvents();
        return user;
    }

    private void SetupValidatorSuccess(LoginCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(LoginCommand command, string propertyName, string errorMessage)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }
}