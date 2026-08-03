using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Ledger.Application.Features.Commands.RefundFunds;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Moq;

namespace Ledger.Application.Tests.Handlers;

public class RefundFundsHandlerTests
{
    private readonly Mock<ILedgerAccountRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<RefundFundsCommand>> _validatorMock = new();
    private readonly Mock<ILogger<RefundFundsHandler>> _loggerMock = new();

    private RefundFundsHandler CreateHandler()
        => new(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldRefundFunds()
    {
        var handler = CreateHandler();
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.AvailableBalance = 1000L;

        var command = new RefundFundsCommand(account.MerchantId, 200L, "USD", "corr-ref");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(800L, account.AvailableBalance);
        Assert.Single(account.Journal, j => j.Type == EntryType.Debit && j.Amount.Amount == 200L);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ShouldFail()
    {
        var handler = CreateHandler();
        var command = new RefundFundsCommand(Guid.NewGuid(), 200L, "USD", "corr");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(command.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);

        var result = await handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.AccountNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InsufficientAvailable_ShouldFail()
    {
        var handler = CreateHandler();
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.AvailableBalance = 10L;

        var command = new RefundFundsCommand(account.MerchantId, 200L, "USD", "corr");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.RefundFailed", result.Errors[0].Code);
    }

    private void SetupValidatorSuccess(RefundFundsCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}
