using Ledger.Application.Features.Queries.GetBalance;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Moq;

namespace Ledger.Application.Tests.Handlers.Queries;

public class GetBalanceHandlerTests
{
    [Fact]
    public async Task Handle_ExistingAccount_ReturnsBalanceDto()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.AvailableBalance = 500L;
        account.PendingBalance = 200L;
        account.ReservedBalance = 0L;

        var repoMock = new Mock<ILedgerAccountRepository>();
        repoMock.Setup(r => r.GetByMerchantIdAsync(account.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var uowMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<GetBalanceHandler>>();
        var handler = new GetBalanceHandler(repoMock.Object, uowMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetBalanceQuery(account.MerchantId));

        Assert.True(result.IsSuccess);
        Assert.Equal(500L, result.Value.Available);
        Assert.Equal(200L, result.Value.Pending);
    }

    [Fact]
    public async Task Handle_NotFound_AutoCreatesAccount()
    {
        var repoMock = new Mock<ILedgerAccountRepository>();
        repoMock.Setup(r => r.GetByMerchantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);
        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var loggerMock = new Mock<ILogger<GetBalanceHandler>>();
        var handler = new GetBalanceHandler(repoMock.Object, uowMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetBalanceQuery(Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        Assert.Equal("NGN", result.Value.Currency);
        repoMock.Verify(r => r.AddAsync(It.IsAny<LedgerAccount>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}