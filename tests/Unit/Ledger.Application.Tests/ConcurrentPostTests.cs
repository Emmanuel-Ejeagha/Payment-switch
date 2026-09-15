using BuildingBlocks.Shared.Exceptions;
using Ledger.Application.Features.Commands.ReserveFunds;
using Ledger.Domain.Entities;
using Ledger.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Ledger.Application.Tests;

public class ConcurrentPostTests
{
    [Fact]
    public async Task ConcurrentReserve_SecondWriter_GetsConcurrencyConflict()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        var repoMock = new Moq.Mock<Ledger.Application.Interfaces.ILedgerAccountRepository>();
        var uowMock = new Moq.Mock<Ledger.Application.Interfaces.IUnitOfWork>();
        var validatorMock = new Moq.Mock<FluentValidation.IValidator<ReserveFundsCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<ReserveFundsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // First save succeeds, second would throw concurrency
        var saveCount = 0;
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => {
                saveCount++;
                if (saveCount == 2) throw new ConcurrencyConflictException();
                return 1;
            });
        uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uowMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uowMock.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new ReserveFundsHandler(repoMock.Object, uowMock.Object, validatorMock.Object, NullLogger<ReserveFundsHandler>.Instance);
        var cmd = new ReserveFundsCommand(account.MerchantId, 100, "USD", "corr-1");

        var result1 = await handler.Handle(cmd);
        Assert.True(result1.IsSuccess);

        // Simulate concurrent second writer on same account (after first committed, second's RowVersion is stale)
        // The handler should catch ConcurrencyConflictException and return ConcurrencyConflict error
        uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ConcurrencyConflictException());
        var result2 = await handler.Handle(new ReserveFundsCommand(account.MerchantId, 100, "USD", "corr-2"));
        Assert.True(result2.IsFailure);
        Assert.Contains(result2.Errors, e => e.Code == "Ledger.ConcurrencyConflict");
    }
}
