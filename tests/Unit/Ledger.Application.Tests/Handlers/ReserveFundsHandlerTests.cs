using FluentValidation;
using FluentValidation.Results;
using Ledger.Application.Features.Commands.ReserveFunds;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Moq;

namespace Ledger.Application.Tests.Handlers;

public class ReserveFundsHandlerTests
{
    private readonly Mock<ILedgerAccountRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<ReserveFundsCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ReserveFundsHandler>> _loggerMock = new();
    private readonly ReserveFundsHandler _handler;

    public ReserveFundsHandlerTests()
    {
        _handler = new ReserveFundsHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReserveFunds()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.AvailableBalance = 200L; // internal setter via InternalsVisibleTo

        var command = new ReserveFundsCommand(account.MerchantId, 100L, "USD", "correlation-1");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync(account);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(200L, account.AvailableBalance);
        Assert.Equal(100L, account.PendingBalance);
        Assert.Equal(100L, account.ReservedBalance);
        Assert.Single(account.Journal, j => j.Type == EntryType.Credit && j.CreditAccount == GlAccountCode.Reserve);
    }

    [Fact]
    public async Task Handle_AccountNotFound_AutoCreatesAccount()
    {
        var command = new ReserveFundsCommand(Guid.NewGuid(), 100L, "USD", "corr");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(command.MerchantId, "USD", It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.AddAsync(It.Is<LedgerAccount>(a => a.Currency == "USD"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CurrencyMismatch_ShouldFail()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.AvailableBalance = 50L;
        var command = new ReserveFundsCommand(account.MerchantId, 100L, "EUR", "corr");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAndCurrencyAsync(account.MerchantId, "EUR", It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.ReserveFailed", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        var command = new ReserveFundsCommand(Guid.Empty, 0, "", "");
        SetupValidatorFailure(command, "Amount", "Amount must be greater than zero.");

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Amount");
    }

    private void SetupValidatorSuccess(ReserveFundsCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupValidatorFailure(ReserveFundsCommand command, string property, string error) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, error) }));
}