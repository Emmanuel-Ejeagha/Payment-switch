using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Ledger.Application.Features.Commands.CaptureFunds;
using Ledger.Application.Interfaces;
using Ledger.Application.Options;
using Ledger.Domain.DomainEvents;
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
    private readonly Mock<ILogger<CaptureFundsHandler>> _loggerMock = new();

    private CaptureFundsHandler CreateHandler(LedgerOptions options)
        => new(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, options, _loggerMock.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCaptureFunds()
    {
        var handler = CreateHandler(new LedgerOptions { FeeBasisPoints = 0 });
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.PendingBalance = 200L; // simulate authorized funds
        account.ReservedBalance = 200L;

        var command = new CaptureFundsCommand(account.MerchantId, 100L, "USD", "corr-2");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(100L, account.AvailableBalance);
        Assert.Equal(100L, account.PendingBalance);
        Assert.Equal(100L, account.ReservedBalance);
        Assert.Single(account.Journal, j => j.Type == EntryType.Credit);
        Assert.DoesNotContain(account.DomainEvents, e => e is FeesChargedEvent);
    }

    [Fact]
    public async Task Handle_ValidCommand_WithFee_ShouldDeductProcessingFee()
    {
        var handler = CreateHandler(new LedgerOptions { FeeBasisPoints = 150 });
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.PendingBalance = 200L;
        account.ReservedBalance = 200L;

        var command = new CaptureFundsCommand(account.MerchantId, 100L, "USD", "corr-2");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(98L, account.AvailableBalance);
        Assert.Equal(100L, account.PendingBalance);
        Assert.Equal(2, account.Journal.Count);
        Assert.Contains(account.Journal, j => j.CreditAccount == GlAccountCode.FeesIncome && j.Amount.Amount == 2L);
        Assert.Contains(account.DomainEvents, e => e is FeesChargedEvent);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ShouldFail()
    {
        var handler = CreateHandler(new LedgerOptions { FeeBasisPoints = 0 });
        var command = new CaptureFundsCommand(Guid.NewGuid(), 100L, "USD", "corr");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(command.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);

        var result = await handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.AccountNotFound", result.Errors[0].Code);
    }

    private void SetupValidatorSuccess(CaptureFundsCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}