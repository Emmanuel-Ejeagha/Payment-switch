using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Email;
using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.Register;
using Identity.Application.Configuration;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IEmailVerificationTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly IOptions<EmailVerificationOptions> _options;
    private readonly Mock<IValidator<RegisterUserCommand>> _validatorMock = new();
    private readonly Mock<ILogger<RegisterUserHandler>> _loggerMock = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _options = Options.Create(new EmailVerificationOptions { FrontendBaseUrl = "http://localhost:3000" });
        _tokenFactoryMock.Setup(f => f.Generate(It.IsAny<TimeSpan>()))
            .Returns(new EmailVerificationTokenData("plain-token", "token-hash", DateTime.UtcNow.AddHours(24)));
        _emailSenderMock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _handler = new RegisterUserHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _dispatcherMock.Object,
            _tokenFactoryMock.Object,
            _emailSenderMock.Object,
            _options,
            _validatorMock.Object, _loggerMock.Object);
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
        Assert.NotEqual(Guid.Empty, result.Value!.UserId);
        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Email.Value == command.Email &&
            !u.EmailConfirmed &&
            u.EmailVerificationTokenHash == "token-hash" &&
            u.EmailVerificationTokenExpiresAt != null), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(s => s.SendAsync(
            It.Is<EmailMessage>(m => m.To == command.Email && m.TextBody.Contains("plain-token")),
            It.IsAny<CancellationToken>()), Times.Once);
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
        _emailSenderMock.Verify(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
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

    [Fact]
    public async Task Handle_EmailSendFailure_ShouldStillRegister()
    {
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
        _emailSenderMock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Error("Email.SendFailed", "SMTP down"));

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupValidatorSuccess(RegisterUserCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }
}
