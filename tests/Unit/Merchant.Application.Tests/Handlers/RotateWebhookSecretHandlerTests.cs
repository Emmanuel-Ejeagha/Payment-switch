using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.RotateWebhookSecret;

namespace Merchant.Application.Tests.Handlers.Commands;

public class RotateWebhookSecretHandlerTests
{
    [Fact]
    public async Task Handle_Owner_ReturnsNewSecretOnce()
    {
        var ownerId = Guid.NewGuid();
        var merchant = CreateActiveMerchant(ownerId);
        var original = merchant.WebhookSecret!.Value;
        var repoMock = new Mock<IMerchantRepository>();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var uowMock = new Mock<IUnitOfWork>();
        var validator = new RotateWebhookSecretCommandValidator();
        var loggerMock = new Mock<ILogger<RotateWebhookSecretHandler>>();
        var handler = new RotateWebhookSecretHandler(repoMock.Object, uowMock.Object, validator, loggerMock.Object);

        var result = await handler.Handle(new RotateWebhookSecretCommand(merchant.Id, new CallerContext(ownerId, null, false, EmailVerified: true)));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(original, result.Value);
        Assert.Equal(result.Value, merchant.WebhookSecret!.Value);
        Assert.Equal(original, merchant.PreviousWebhookSecret!.Value);
        Assert.NotNull(merchant.WebhookSecretRotatedAtUtc);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnverifiedOwner_ReturnsEmailNotVerified()
    {
        var ownerId = Guid.NewGuid();
        var merchant = CreateActiveMerchant(ownerId);
        var repoMock = new Mock<IMerchantRepository>();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var uowMock = new Mock<IUnitOfWork>();
        var validator = new RotateWebhookSecretCommandValidator();
        var loggerMock = new Mock<ILogger<RotateWebhookSecretHandler>>();
        var handler = new RotateWebhookSecretHandler(repoMock.Object, uowMock.Object, validator, loggerMock.Object);

        var result = await handler.Handle(new RotateWebhookSecretCommand(merchant.Id, new CallerContext(ownerId, null, false)));

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.EmailNotVerified", result.Errors[0].Code);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonActiveMerchant_ReturnsNotActive()
    {
        var ownerId = Guid.NewGuid();
        var merchant = new MerchantEntity(Guid.NewGuid(), ownerId, new BusinessName("Acme"), new MerchantEmail("acme@test.com"));
        var repoMock = new Mock<IMerchantRepository>();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var uowMock = new Mock<IUnitOfWork>();
        var validator = new RotateWebhookSecretCommandValidator();
        var loggerMock = new Mock<ILogger<RotateWebhookSecretHandler>>();
        var handler = new RotateWebhookSecretHandler(repoMock.Object, uowMock.Object, validator, loggerMock.Object);

        var result = await handler.Handle(new RotateWebhookSecretCommand(merchant.Id, new CallerContext(ownerId, null, false, EmailVerified: true)));

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.NotActive", result.Errors[0].Code);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var merchant = CreateActiveMerchant(ownerId);
        var repoMock = new Mock<IMerchantRepository>();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var uowMock = new Mock<IUnitOfWork>();
        var validator = new RotateWebhookSecretCommandValidator();
        var loggerMock = new Mock<ILogger<RotateWebhookSecretHandler>>();
        var handler = new RotateWebhookSecretHandler(repoMock.Object, uowMock.Object, validator, loggerMock.Object);

        var result = await handler.Handle(new RotateWebhookSecretCommand(merchant.Id, new CallerContext(Guid.NewGuid(), null, false, EmailVerified: true)));

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.Unauthorized", result.Errors[0].Code);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsMerchantNotFound()
    {
        var repoMock = new Mock<IMerchantRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);
        var uowMock = new Mock<IUnitOfWork>();
        var validator = new RotateWebhookSecretCommandValidator();
        var loggerMock = new Mock<ILogger<RotateWebhookSecretHandler>>();
        var handler = new RotateWebhookSecretHandler(repoMock.Object, uowMock.Object, validator, loggerMock.Object);

        var result = await handler.Handle(new RotateWebhookSecretCommand(Guid.NewGuid(), new CallerContext(Guid.NewGuid(), null, false, EmailVerified: true)));

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }

    private static MerchantEntity CreateActiveMerchant(Guid ownerId)
    {
        var merchant = new MerchantEntity(Guid.NewGuid(), ownerId, new BusinessName("Acme"), new MerchantEmail("acme@test.com"));
        merchant.Approve();
        merchant.Activate();
        merchant.ClearDomainEvents();
        return merchant;
    }
}
