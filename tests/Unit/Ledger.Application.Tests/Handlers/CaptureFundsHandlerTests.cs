using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Ledger.Application.Features.Commands.CaptureFunds;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Moq;

namespace Ledger.Application.Tests.Handlers;

public class CaptureFundsHandlerTests
{
    private readonly Mock<ILedgerAccountRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<CaptureFundsCommand>> _validatorMock = new();
    private readonly CaptureFundsHandler _handler;

    public CaptureFundsHandlerTests()
    {
        _handler = new CaptureFundsHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCaptureFunds()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.PendingBalance = 200m; // simulate authorized funds

        var command = new CaptureFundsCommand(account.MerchantId, 100m, "USD", "corr-2");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAsync(account.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, account.AvailableBalance);
        Assert.Equal(100m, account.PendingBalance);
        Assert.Single(account.Journal, j => j.Type == EntryType.Credit);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ShouldFail()
    {
        var command = new CaptureFundsCommand(Guid.NewGuid(), 100m, "USD", "corr");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAsync(command.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.AccountNotFound", result.Errors[0].Code);
    }

    private void SetupValidatorSuccess(CaptureFundsCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}