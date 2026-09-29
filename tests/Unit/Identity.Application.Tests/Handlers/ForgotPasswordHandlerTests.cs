using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Email;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.ForgotPassword;
using Identity.Application.Configuration;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class ForgotPasswordHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmailVerificationTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly IOptions<PasswordResetOptions> _options;
    private readonly Mock<IValidator<ForgotPasswordCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ForgotPasswordHandler>> _loggerMock = new();
    private readonly ForgotPasswordHandler _handler;

    public ForgotPasswordHandlerTests()
    {
        _options = Options.Create(new PasswordResetOptions { FrontendBaseUrl = "http://localhost:3000" });
        _tokenFactoryMock.Setup(f => f.Generate(It.IsAny<TimeSpan>()))
            .Returns(new EmailVerificationTokenData("reset-plain-token", "reset-token-hash", DateTime.UtcNow.AddHours(1)));
        _emailSenderMock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _handler = new ForgotPasswordHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tokenFactoryMock.Object,
            _emailSenderMock.Object,
            _options,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_KnownUser_ShouldStoreResetTokenAndEmail()
    {
        var command = new ForgotPasswordCommand("test@example.com");
        var user = CreateUser();
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("reset-token-hash", user.PasswordResetTokenHash);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(s => s.SendAsync(
            It.Is<EmailMessage>(m => m.To == command.Email && m.TextBody.Contains("reset-plain-token")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownUser_ShouldSucceedSilently()
    {
        var command = new ForgotPasswordCommand("missing@example.com");
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ShouldSucceedSilentlyWithoutIssuingToken()
    {
        var command = new ForgotPasswordCommand("test@example.com");
        var user = CreateUser();
        user.Deactivate();
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Null(user.PasswordResetTokenHash);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ShouldReturnValidationError()
    {
        var command = new ForgotPasswordCommand("not-an-email");
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Email", "A valid email address is required.") }));
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Email", result.Errors[0].Code);
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), new Email("test@example.com"), new PasswordHash("hash"), new FullName("Test User"));
    }
}
