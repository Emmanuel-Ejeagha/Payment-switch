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
        _tokenServiceMock.Setup(t => t.HashRefreshToken(It.IsAny<string>()))
            .Returns<string>(token => $"hash-{token}");
        _tokenServiceMock.Setup(t => t.AccessTokenExpirationSeconds).Returns(3600);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("access_token", result.Value!.AccessToken);
        Assert.Equal("refresh_token", result.Value!.RefreshToken);
        Assert.Equal(3600, result.Value!.ExpiresIn);
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

    [Fact]
    public async Task Handle_LockedAccount_ShouldReturnAccountLocked()
    {
        var command = new LoginCommand("user@example.com", "Password123");
        var user = CreateActiveUser("user@example.com");
        for (var i = 0; i < User.MaxAccessFailedAttempts; i++)
            user.RegisterFailedLogin(DateTime.UtcNow);
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(true);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.AccountLocked", result.Errors[0].Code);
        _passwordHasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<PasswordHash>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LockedAccountWithWrongPassword_ShouldReturnGenericErrorWithoutExtendingLockout()
    {
        var command = new LoginCommand("user@example.com", "WrongPassword");
        var user = CreateActiveUser("user@example.com");
        for (var i = 0; i < User.MaxAccessFailedAttempts; i++)
            user.RegisterFailedLogin(DateTime.UtcNow);
        var lockoutEnd = user.LockoutEnd;
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(false);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidCredentials", result.Errors[0].Code);
        Assert.Equal(lockoutEnd, user.LockoutEnd);
        Assert.Equal(0, user.AccessFailedCount);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongPassword_ShouldIncrementFailedCountAndPersist()
    {
        var command = new LoginCommand("user@example.com", "WrongPassword");
        var user = CreateActiveUser("user@example.com");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(false);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidCredentials", result.Errors[0].Code);
        Assert.Equal(1, user.AccessFailedCount);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepeatedWrongPasswords_ShouldLockAccount()
    {
        var command = new LoginCommand("user@example.com", "WrongPassword");
        var user = CreateActiveUser("user@example.com");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(false);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        for (var i = 0; i < User.MaxAccessFailedAttempts; i++)
            await _handler.Handle(command);

        Assert.True(user.IsLockedOut(DateTime.UtcNow));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(User.MaxAccessFailedAttempts));
    }

    [Fact]
    public async Task Handle_ValidCredentials_ShouldResetFailedCount()
    {
        var command = new LoginCommand("user@example.com", "Password123");
        var user = CreateActiveUser("user@example.com");
        user.RegisterFailedLogin(DateTime.UtcNow);
        user.RegisterFailedLogin(DateTime.UtcNow);
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.Verify(command.Password, user.PasswordHash))
            .Returns(true);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("access_token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh_token");
        _tokenServiceMock.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns<string>(t => $"hash-{t}");
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
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