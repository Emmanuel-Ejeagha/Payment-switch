using Ledger.Application.Features.Commands.RunReconciliation;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ledger.Application.Tests.Handlers;

public class RunReconciliationHandlerTests
{
    private readonly Mock<IReconciliationService> _serviceMock = new();
    private readonly Mock<IReconciliationReportRepository> _reportsMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<RunReconciliationHandler>> _loggerMock = new();

    private RunReconciliationHandler CreateHandler()
        => new(_serviceMock.Object, _reportsMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WithMatchingAccounts_ReturnsCompletedReport()
    {
        _serviceMock.Setup(s => s.ComputeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationLineItem>
            {
                new(Guid.NewGuid(), "USD", 5000, 5000, 0, 0, 0, 0),
                new(Guid.NewGuid(), "EUR", 100, 100, 300, 300, 300, 300)
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new RunReconciliationCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(ReconciliationStatus.Completed, result.Value.Status);
        Assert.Equal(2, result.Value.TotalAccounts);
        Assert.Equal(0, result.Value.MismatchCount);
        Assert.All(result.Value.Items, i => Assert.True(i.IsMatch));
        _reportsMock.Verify(r => r.AddAsync(It.IsAny<ReconciliationReport>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMismatchedAccounts_ReturnsMismatchFoundReport()
    {
        _serviceMock.Setup(s => s.ComputeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationLineItem>
            {
                new(Guid.NewGuid(), "USD", 5000, 5000, 0, 0, 0, 0),
                new(Guid.NewGuid(), "USD", 5000, 4998, 0, 0, 0, 0)
            });

        var handler = CreateHandler();
        var result = await handler.Handle(new RunReconciliationCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(ReconciliationStatus.MismatchFound, result.Value.Status);
        Assert.Equal(1, result.Value.MismatchCount);
        Assert.Single(result.Value.Items, i => i.IsMatch);
        Assert.Single(result.Value.Items, i => !i.IsMatch);
    }

    [Fact]
    public async Task Handle_WithNoAccounts_ReturnsCompletedEmptyReport()
    {
        _serviceMock.Setup(s => s.ComputeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationLineItem>());

        var handler = CreateHandler();
        var result = await handler.Handle(new RunReconciliationCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(ReconciliationStatus.Completed, result.Value.Status);
        Assert.Equal(0, result.Value.TotalAccounts);
    }
}