using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.VerifyEmail;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class VerifyEmailHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmailVerificationTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IValidator<VerifyEmailCommand>> _validatorMock = new();
    private readonly Mock<ILogger<VerifyEmailHandler>> _loggerMock = new();
    private readonly VerifyEmailHandler _handler;

    public VerifyEmailHandlerTests()
    {
        _tokenFactoryMock.Setup(f => f.Hash(It.IsAny<string>())).Returns((string t) => $"sha256({t})");
        _handler = new VerifyEmailHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tokenFactoryMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldConfirmEmailAndClearToken()
    {
        var command = new VerifyEmailCommand("test@example.com", "plain-token");
        var user = CreateUser(false, "sha256(plain-token)", DateTime.UtcNow.AddHours(1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.True(user.EmailConfirmed);
        Assert.Null(user.EmailVerificationTokenHash);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnError()
    {
        var command = new VerifyEmailCommand("test@example.com", "wrong-token");
        var user = CreateUser(false, "sha256(right-token)", DateTime.UtcNow.AddHours(1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidVerificationToken", result.Errors[0].Code);
        Assert.False(user.EmailConfirmed);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ShouldReturnError()
    {
        var command = new VerifyEmailCommand("test@example.com", "plain-token");
        var user = CreateUser(false, "sha256(plain-token)", DateTime.UtcNow.AddHours(-1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.VerificationTokenExpired", result.Errors[0].Code);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_TokenAlreadyUsed_SecondUseRejected()
    {
        var command = new VerifyEmailCommand("test@example.com", "plain-token");
        var user = CreateUser(false, "sha256(plain-token)", DateTime.UtcNow.AddHours(1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var first = await _handler.Handle(command);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(command);
        Assert.True(second.IsFailure);
        Assert.Equal("Identity.EmailAlreadyVerified", second.Errors[0].Code);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task Handle_UnknownUser_ShouldReturnNotFound()
    {
        var command = new VerifyEmailCommand("missing@example.com", "plain-token");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.UserNotFound", result.Errors[0].Code);
    }

    private void SetupValidatorSuccess(VerifyEmailCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static User CreateUser(bool confirmed, string? tokenHash, DateTime? expiresAt)
    {
        var user = new User(Guid.NewGuid(), new Email("test@example.com"), new PasswordHash("hash"), new FullName("Test User"));
        if (confirmed)
        {
            user.MarkEmailConfirmed();
            return user;
        }

        if (tokenHash is not null && expiresAt is not null)
            user.InitiateEmailVerification(tokenHash, expiresAt.Value);
        return user;
    }
}
