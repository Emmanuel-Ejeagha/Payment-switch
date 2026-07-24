using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.Tokens;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class RefreshTokenHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<RefreshTokenCommand>> _validatorMock = new();
    private readonly Mock<ILogger<RefreshTokenHandler>> _loggerMock = new();
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _handler = new RefreshTokenHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldRotateAndReturnNewTokens()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid_refresh_token");
        var user = CreateUserWithRefreshToken("valid_refresh_token");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.FindByRefreshTokenAsync("valid_refresh_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user))
            .Returns("new_access_token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns("new_refresh_token");
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new_access_token", result.Value.AccessToken);
        Assert.Equal("new_refresh_token", result.Value.RefreshToken);
        Assert.True(user.RefreshTokens.First(t => t.Value == "valid_refresh_token").IsRevoked);
        Assert.Contains(user.RefreshTokens, t => t.Value == "new_refresh_token");
    }

    [Fact]
    public async Task Handle_ExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("expired_token");
        var user = CreateUserWithExpiredRefreshToken("expired_token");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.FindByRefreshTokenAsync("expired_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.RefreshTokenInvalidOrExpired", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_RevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("revoked_token");
        var user = CreateUserWithRefreshToken("revoked_token");
        user.RevokeRefreshToken("revoked_token"); // mark revoked
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.FindByRefreshTokenAsync("revoked_token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.RefreshTokenInvalidOrExpired", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new RefreshTokenCommand("nonexistent");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.FindByRefreshTokenAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidRefreshToken", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        // Arrange
        var command = new RefreshTokenCommand("");
        SetupValidatorFailure(command, "RefreshToken", "'Refresh Token' must not be empty.");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "RefreshToken");
    }

    private static User CreateUserWithRefreshToken(string tokenValue)
    {
        var user = new User(Guid.NewGuid(), new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));
        user.AddRefreshToken(tokenValue, DateTime.UtcNow.AddDays(1));
        user.ClearDomainEvents();
        return user;
    }

    private static User CreateUserWithExpiredRefreshToken(string tokenValue)
    {
        var user = new User(Guid.NewGuid(), new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));
        user.AddRefreshToken(tokenValue, DateTime.UtcNow.AddDays(-1)); // expired
        user.ClearDomainEvents();
        return user;
    }

    private void SetupValidatorSuccess(RefreshTokenCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(RefreshTokenCommand command, string propertyName, string errorMessage)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }
}