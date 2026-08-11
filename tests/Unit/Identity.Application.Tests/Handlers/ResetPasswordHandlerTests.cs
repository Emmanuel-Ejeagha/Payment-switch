using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.ResetPassword;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class ResetPasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IEmailVerificationTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IValidator<ResetPasswordCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ResetPasswordHandler>> _loggerMock = new();
    private readonly ResetPasswordHandler _handler;

    public ResetPasswordHandlerTests()
    {
        _tokenFactoryMock.Setup(f => f.Hash(It.IsAny<string>())).Returns((string t) => $"sha256({t})");
        _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns(new PasswordHash("hashed-new"));
        _handler = new ResetPasswordHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _tokenFactoryMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ShouldResetPasswordAndSave()
    {
        var command = new ResetPasswordCommand("test@example.com", "plain-token", "NewPassw0rd!");
        var user = CreateUser();
        user.InitiatePasswordReset("sha256(plain-token)", DateTime.UtcNow.AddHours(1));
        user.AddRefreshToken("old-session", DateTime.UtcNow.AddDays(1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(new PasswordHash("hashed-new"), user.PasswordHash);
        Assert.Null(user.PasswordResetTokenHash);
        Assert.All(user.RefreshTokens, t => Assert.True(t.IsRevoked));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnError()
    {
        var command = new ResetPasswordCommand("test@example.com", "wrong-token", "NewPassw0rd!");
        var user = CreateUser();
        user.InitiatePasswordReset("sha256(right-token)", DateTime.UtcNow.AddHours(1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidPasswordResetToken", result.Errors[0].Code);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ShouldReturnExpiredError()
    {
        var command = new ResetPasswordCommand("test@example.com", "plain-token", "NewPassw0rd!");
        var user = CreateUser();
        user.InitiatePasswordReset("sha256(plain-token)", DateTime.UtcNow.AddHours(-1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.PasswordResetTokenExpired", result.Errors[0].Code);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownUser_ShouldReturnGenericError()
    {
        var command = new ResetPasswordCommand("missing@example.com", "plain-token", "NewPassw0rd!");
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidPasswordResetToken", result.Errors[0].Code);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupValidatorSuccess(ResetPasswordCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("test@example.com"), new PasswordHash("hash"), new FullName("Test User"));
    }
}
