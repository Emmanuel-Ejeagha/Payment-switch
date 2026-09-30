using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Auth.ResendVerification;
using Identity.Application.Configuration;
using Identity.Application.Interfaces;
using Identity.Domain.DomainEvents;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class ResendVerificationHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEmailVerificationTokenFactory> _tokenFactoryMock = new();
    private readonly IOptions<EmailVerificationOptions> _options;
    private readonly Mock<IValidator<ResendVerificationCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ResendVerificationHandler>> _loggerMock = new();
    private readonly ResendVerificationHandler _handler;

    public ResendVerificationHandlerTests()
    {
        _options = Options.Create(new EmailVerificationOptions { FrontendBaseUrl = "http://localhost:3000" });
        _tokenFactoryMock.Setup(f => f.Generate(It.IsAny<TimeSpan>()))
            .Returns(new EmailVerificationTokenData("new-plain-token", "new-token-hash", DateTime.UtcNow.AddHours(24)));

        _handler = new ResendVerificationHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tokenFactoryMock.Object,
            _options,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_UnverifiedUser_ShouldIssueNewTokenAndRaiseEvent()
    {
        var command = new ResendVerificationCommand("test@example.com");
        var user = CreateUser(false, "old-hash", DateTime.UtcNow.AddHours(1));
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        // Delivery is event-driven (outbox → Notification/Resend): rotation is
        // recorded, the event raised, and nothing is sent inline.
        Assert.True(result.IsSuccess);
        Assert.Equal("new-token-hash", user.EmailVerificationTokenHash);
        Assert.NotNull(user.LastVerificationEmailSentAtUtc);
        // Setup issuance raised one event; the resend must raise a second one
        // carrying the fresh token (rotation).
        var raised = user.DomainEvents.OfType<EmailVerificationRequestedDomainEvent>().ToList();
        Assert.Equal(2, raised.Count);
        Assert.Equal("new-plain-token", raised.Last().Token);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyVerified_ShouldSucceedNeutrally()
    {
        var command = new ResendVerificationCommand("test@example.com");
        var user = CreateUser(true, null, null);
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command);

        // Neutral: must not disclose that the address is registered + verified.
        Assert.True(result.IsSuccess);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownUser_ShouldSucceedNeutrally()
    {
        var command = new ResendVerificationCommand("missing@example.com");
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(command);

        // Neutral: must not disclose that the address is unregistered.
        Assert.True(result.IsSuccess);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RecentSend_ShouldThrottleWithoutRotating()
    {
        var command = new ResendVerificationCommand("test@example.com");
        var user = CreateUser(false, "old-hash", DateTime.UtcNow.AddHours(1));
        user.RecordVerificationEmailSent();
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.VerificationResendThrottled", result.Errors[0].Code);
        Assert.Equal("old-hash", user.EmailVerificationTokenHash);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AgedSend_ShouldSucceedAndRotate()
    {
        var command = new ResendVerificationCommand("test@example.com");
        var user = CreateUser(false, "old-hash", DateTime.UtcNow.AddHours(1));
        user.RecordVerificationEmailSent(DateTime.UtcNow.AddMinutes(-5));
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("new-token-hash", user.EmailVerificationTokenHash);
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
            user.InitiateEmailVerification(tokenHash, "plain-token", expiresAt.Value);
        return user;
    }
}
