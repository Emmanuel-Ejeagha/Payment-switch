using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Admin;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class UnsuspendUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IValidator<UnsuspendUserCommand>> _validatorMock = new();
    private readonly Mock<ILogger<UnsuspendUserHandler>> _loggerMock = new();
    private readonly UnsuspendUserHandler _handler;

    public UnsuspendUserHandlerTests()
    {
        _handler = new UnsuspendUserHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_AdminUnsuspendsUser_Reactivates()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var command = new UnsuspendUserCommand(adminId, targetId);
        var adminUser = CreateUserWithRoles(adminId, new[] { "Admin" });
        var targetUser = CreateUserWithRoles(targetId, new[] { "Merchant" });
        targetUser.Deactivate();

        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.True(targetUser.IsActive);
    }

    [Fact]
    public async Task Handle_NonAdminCaller_ReturnsNotAuthorized()
    {
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var command = new UnsuspendUserCommand(callerId, targetId);
        var caller = CreateUserWithRoles(callerId, new[] { "Merchant" });
        var targetUser = CreateUserWithRoles(targetId, new[] { "Merchant" });
        targetUser.Deactivate();

        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(caller);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Identity.NotAuthorized", result.Errors[0].Code);
        Assert.False(targetUser.IsActive);
    }

    private static User CreateUserWithRoles(Guid id, string[] roles)
    {
        var user = new User(id, new Email("user@example.com"), new PasswordHash("hashed"), new FullName("User"));
        foreach (var role in roles)
            user.AddRole(role);
        user.ClearDomainEvents();
        return user;
    }

    private void SetupValidatorSuccess(UnsuspendUserCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
}
