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
        account.AvailableBalance = 500m;
        account.PendingBalance = 200m;
        account.ReservedBalance = 0m;

        var repoMock = new Mock<ILedgerAccountRepository>();
        repoMock.Setup(r => r.GetByMerchantIdAsync(account.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        var loggerMock = new Mock<ILogger<GetBalanceHandler>>();
        var handler = new GetBalanceHandler(repoMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetBalanceQuery(account.MerchantId));

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.Available);
        Assert.Equal(200m, result.Value.Pending);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        var repoMock = new Mock<ILedgerAccountRepository>();
        repoMock.Setup(r => r.GetByMerchantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);
        var loggerMock = new Mock<ILogger<GetBalanceHandler>>();
        var handler = new GetBalanceHandler(repoMock.Object, loggerMock.Object);

        var result = await handler.Handle(new GetBalanceQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.AccountNotFound", result.Errors[0].Code);
    }
}