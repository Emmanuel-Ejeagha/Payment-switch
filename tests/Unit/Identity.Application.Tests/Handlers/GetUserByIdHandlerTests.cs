using Identity.Application.Interfaces;
using Identity.Application.Queries.User;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Moq;

namespace Identity.Application.Tests.Handlers;

public class GetUserByIdHandlerTests
{
    [Fact]
    public async Task Handle_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), new Email("test@example.com"), new PasswordHash("hashed"), new FullName("Test User"));
        user.AddRole("Admin");
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var loggerMock = new Mock<ILogger<GetUserByIdHandler>>();
        var handler = new GetUserByIdHandler(repoMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(new GetUserByIdQuery(user.Id));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.Contains("Admin", result.Value.Roles);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var loggerMock = new Mock<ILogger<GetUserByIdHandler>>();
        var handler = new GetUserByIdHandler(repoMock.Object, loggerMock.Object);

        // Act
        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Identity.UserNotFound", result.Errors[0].Code);
    }
}