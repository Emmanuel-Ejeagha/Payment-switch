using FluentValidation;
using FluentValidation.Results;
using Identity.Application.Commands.Role;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class AssignRoleHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IValidator<AssignRoleCommand>> _validatorMock = new();
    private readonly Mock<ILogger<AssignRoleHandler>> _loggerMock = new();
    private readonly AssignRoleHandler _handler;

    public AssignRoleHandlerTests()
    {
        _handler = new AssignRoleHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_AdminAssignsRole_ShouldSucceed()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var command = new AssignRoleCommand(adminId, targetId, "Support");
        var adminUser = CreateUserWithRoles(adminId, new[] { "Admin" });
        var targetUser = CreateUserWithRoles(targetId, new[] { "Merchant" });

        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(adminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Support", targetUser.Roles);
    }

    [Fact]
    public async Task Handle_NonAdminUser_ShouldReturnFailure()
    {
        // Arrange
        var nonAdminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var command = new AssignRoleCommand(nonAdminId, targetId, "Support");
        var nonAdminUser = CreateUserWithRoles(nonAdminId, new[] { "Merchant" });
        var targetUser = CreateUserWithRoles(targetId, new[] { "Merchant" });

        SetupValidatorSuccess(command);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(nonAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonAdminUser);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.NotAuthorized", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidRole_ShouldReturnValidationErrors()
    {
        // Arrange
        var command = new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "InvalidRole");
        SetupValidatorFailure(command, "Role", "Role must be one of: Admin, Merchant, Support.");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Role");
    }

    private static User CreateUserWithRoles(Guid userId, string[] roles)
    {
        var user = new User(userId, new Email($"{userId}@example.com"), new PasswordHash("hashed"), new FullName("User"));
        foreach (var role in roles)
            user.AddRole(role);
        user.ClearDomainEvents();
        return user;
    }

    private void SetupValidatorSuccess(AssignRoleCommand command)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void SetupValidatorFailure(AssignRoleCommand command, string propertyName, string errorMessage)
    {
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(propertyName, errorMessage) }));
    }
}