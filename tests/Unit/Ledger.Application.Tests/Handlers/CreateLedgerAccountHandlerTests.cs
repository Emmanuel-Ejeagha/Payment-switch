using FluentValidation;
using FluentValidation.Results;
using Ledger.Application.Features.Commands.CreateLedgerAccount;
using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Moq;

namespace Ledger.Application.Tests.Handlers;

public class CreateLedgerAccountHandlerTests
{
    private readonly Mock<ILedgerAccountRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<CreateLedgerAccountCommand>> _validatorMock = new();
    private readonly Mock<ILogger<CreateLedgerAccountHandler>> _loggerMock = new();
    private readonly CreateLedgerAccountHandler _handler;

    public CreateLedgerAccountHandlerTests()
    {
        _handler = new CreateLedgerAccountHandler(_repoMock.Object, _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoExistingAccount_ShouldCreate()
    {
        var command = new CreateLedgerAccountCommand(Guid.NewGuid(), "USD");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAsync(command.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync((LedgerAccount?)null);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<LedgerAccount>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingAccount_ShouldReturnSuccess()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        var command = new CreateLedgerAccountCommand(account.MerchantId, "USD");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByMerchantIdAsync(account.MerchantId, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<LedgerAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupValidatorSuccess(CreateLedgerAccountCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}