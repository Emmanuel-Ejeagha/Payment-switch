using Merchant.Application.Auth;
using Merchant.Application.Features.Queries.GetMerchantById;


namespace Merchant.Application.Tests.Handlers.Queries;

public class GetMerchantByIdHandlerTests
{
    [Fact]
    public async Task Handle_MerchantExists_ReturnsDto()
    {
        var repoMock = new Mock<IMerchantRepository>();
        var merchant = new MerchantEntity(Guid.NewGuid(), new BusinessName("Acme"), new MerchantEmail("acme@test.com"));
        merchant.Approve();
        merchant.Activate();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var loggerMock = new Mock<ILogger<GetMerchantByIdHandler>>();
        var handler = new GetMerchantByIdHandler(repoMock.Object, loggerMock.Object);

        var caller = new CallerContext(Guid.NewGuid(), null, false);
        var result = await handler.Handle(new GetMerchantByIdQuery(merchant.Id, caller));

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.Unauthorized", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsDto()
    {
        var repoMock = new Mock<IMerchantRepository>();
        var ownerId = Guid.NewGuid();
        var merchant = new MerchantEntity(Guid.NewGuid(), ownerId, new BusinessName("Acme"), new MerchantEmail("acme@test.com"));
        merchant.Approve();
        merchant.Activate();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var loggerMock = new Mock<ILogger<GetMerchantByIdHandler>>();
        var handler = new GetMerchantByIdHandler(repoMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetMerchantByIdQuery(merchant.Id, new CallerContext(ownerId, null, false)));

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme", result.Value!.BusinessName);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task Handle_Admin_ReturnsDto()
    {
        var repoMock = new Mock<IMerchantRepository>();
        var merchant = new MerchantEntity(Guid.NewGuid(), new BusinessName("Acme"), new MerchantEmail("acme@test.com"));
        merchant.Approve();
        merchant.Activate();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var loggerMock = new Mock<ILogger<GetMerchantByIdHandler>>();
        var handler = new GetMerchantByIdHandler(repoMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetMerchantByIdQuery(merchant.Id, new CallerContext(null, null, true)));

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme", result.Value!.BusinessName);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        var repoMock = new Mock<IMerchantRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);
        var loggerMock = new Mock<ILogger<GetMerchantByIdHandler>>();
        var handler = new GetMerchantByIdHandler(repoMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetMerchantByIdQuery(Guid.NewGuid(), new CallerContext(Guid.NewGuid(), null, false)));

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }
}