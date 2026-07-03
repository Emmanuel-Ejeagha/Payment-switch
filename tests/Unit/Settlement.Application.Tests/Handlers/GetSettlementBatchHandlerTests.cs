using Moq;
using Settlement.Application.Features.Queries.GetSettlementBatch;
using Settlement.Application.Interfaces;
using Settlement.Domain.Entities;
using Settlement.Domain.ValueObjects;

namespace Settlement.Application.Tests.Handlers.Queries;

public class GetSettlementBatchHandlerTests
{
    [Fact]
    public async Task Handle_Found_ReturnsDto()
    {
        var batch = new SettlementBatch(Guid.NewGuid(), new DateTime(2026, 7, 3));
        batch.AddPayout(Guid.NewGuid(), new Money(100, "USD"), new Money(5, "USD"));
        var repoMock = new Mock<ISettlementBatchRepository>();
        repoMock.Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>())).ReturnsAsync(batch);
        var handler = new GetSettlementBatchHandler(repoMock.Object);

        var result = await handler.Handle(new GetSettlementBatchQuery(batch.Id));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Payouts);
        Assert.Equal(95m, result.Value.TotalAmount);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        var repoMock = new Mock<ISettlementBatchRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((SettlementBatch?)null);
        var handler = new GetSettlementBatchHandler(repoMock.Object);

        var result = await handler.Handle(new GetSettlementBatchQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Settlement.BatchNotFound", result.Errors[0].Code);
    }
}