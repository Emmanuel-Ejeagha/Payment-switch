using Merchant.Application.Features.Queries.GetMerchantById;


namespace Merchant.Application.Tests.Handlers.Queries;

public class GetMerchantByIdHandlerTests
{
    [Fact]
    public async Task Handle_MerchantExists_ReturnsDto()
    {
        var repoMock = new Mock<IMerchantRepository>();
        var merchant = new MerchantEntity(Guid.NewGuid(), new BusinessName("Acme"), new MerchantEmail("acme@test.com"));
        merchant.Activate();
        repoMock.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(merchant);
        var handler = new GetMerchantByIdHandler(repoMock.Object);

        var result = await handler.Handle(new GetMerchantByIdQuery(merchant.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme", result.Value!.BusinessName);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        var repoMock = new Mock<IMerchantRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((MerchantEntity?)null);
        var handler = new GetMerchantByIdHandler(repoMock.Object);

        var result = await handler.Handle(new GetMerchantByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Merchant.MerchantNotFound", result.Errors[0].Code);
    }
}