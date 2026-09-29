using BuildingBlocks.Shared.Results;
using Identity.Application.Commands.Auth.Tokens;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class RevokeRefreshTokenHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<RevokeRefreshTokenHandler>> _loggerMock = new();
    private readonly RevokeRefreshTokenHandler _handler;

    public RevokeRefreshTokenHandlerTests()
    {
        _handler = new RevokeRefreshTokenHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        _tokenServiceMock.Setup(t => t.HashRefreshToken(It.IsAny<string>()))
            .Returns<string>(token => $"hash-{token}");
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldRevoke()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));
        user.AddRefreshToken("hash-token123", DateTime.UtcNow.AddDays(1));
        user.ClearDomainEvents();
        var command = new RevokeRefreshTokenCommand("token123");

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.RefreshTokens.Single(t => t.Value == "hash-token123").IsRevoked);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RevokeRefreshTokenCommand("token123");
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.UserNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_UnknownToken_ShouldSucceedWithoutError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));
        user.ClearDomainEvents();
        var command = new RevokeRefreshTokenCommand("nonexistent");

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
