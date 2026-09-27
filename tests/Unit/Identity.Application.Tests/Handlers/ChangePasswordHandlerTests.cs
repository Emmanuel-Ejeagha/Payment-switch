using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.ChangePassword;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class ChangePasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IValidator<ChangePasswordCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ChangePasswordHandler>> _loggerMock = new();
    private readonly ChangePasswordHandler _handler;

    public ChangePasswordHandlerTests()
    {
        _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns(new PasswordHash("hashed-new"));
        _handler = new ChangePasswordHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_ShouldChangeHashAndRevokeSessions()
    {
        var command = new ChangePasswordCommand("OldPassw0rd!", "NewPassw0rd!");
        var user = CreateUser();
        user.AddRefreshToken("old-session", DateTime.UtcNow.AddDays(1));
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.Verify(command.CurrentPassword, user.PasswordHash)).Returns(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(new PasswordHash("hashed-new"), user.PasswordHash);
        Assert.All(user.RefreshTokens, t => Assert.True(t.IsRevoked));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ShouldReturnError()
    {
        var command = new ChangePasswordCommand("WrongPassw0rd!", "NewPassw0rd!");
        var user = CreateUser();
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.Verify(command.CurrentPassword, user.PasswordHash)).Returns(false);

        var result = await _handler.Handle(command, user.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.InvalidCurrentPassword", result.Errors[0].Code);
        Assert.NotEqual(new PasswordHash("hashed-new"), user.PasswordHash);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownUser_ShouldReturnNotFound()
    {
        var command = new ChangePasswordCommand("OldPassw0rd!", "NewPassw0rd!");
        var userId = Guid.NewGuid();
        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command, userId);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.UserNotFound", result.Errors[0].Code);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidationFailure_ShouldReturnErrors()
    {
        var command = new ChangePasswordCommand("OldPassw0rd!", "OldPassw0rd!");
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("NewPassword", "New password must be different from the current password.") }));
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("NewPassword", result.Errors[0].Code);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupValidatorSuccess(ChangePasswordCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("test@example.com"), new PasswordHash("hash"), new FullName("Test User"));
    }
}
