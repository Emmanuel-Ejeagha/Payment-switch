using FluentValidation;
using FluentValidation.Results;
using Ledger.Application.Features.Commands.ReleaseFunds;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Moq;

namespace Ledger.Application.Tests.Handlers;

public class ReleaseFundsHandlerTests
{
    private readonly Mock<ILedgerAccountRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<ReleaseFundsCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ReleaseFundsHandler>> _loggerMock = new();
    private readonly ReleaseFundsHandler _handler;

    public ReleaseFundsHandlerTests()
    {
        _handler = new ReleaseFundsHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReleaseReservation()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.PendingBalance = 500L;
        account.ReservedBalance = 500L;
        account.ClearDomainEvents();

        var command = new ReleaseFundsCommand(account.MerchantId, 500L, "USD", "PaymentVoid:1");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(0L, account.PendingBalance);
        Assert.Equal(0L, account.ReservedBalance);
        Assert.Equal(0L, account.AvailableBalance);
        Assert.Single(account.Journal, j => j.Type == EntryType.Debit && j.DebitAccount == GlAccountCode.Reserve);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ShouldFail()
    {
        var command = new ReleaseFundsCommand(Guid.NewGuid(), 100L, "USD", "PaymentVoid:1");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(command.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OverRelease_ShouldFail()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.PendingBalance = 100L;
        account.ReservedBalance = 100L;
        account.ClearDomainEvents();

        var command = new ReleaseFundsCommand(account.MerchantId, 500L, "USD", "PaymentVoid:1");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal(100L, account.PendingBalance);
        Assert.Equal(100L, account.ReservedBalance);
    }

    private void SetupValidatorSuccess(ReleaseFundsCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}
