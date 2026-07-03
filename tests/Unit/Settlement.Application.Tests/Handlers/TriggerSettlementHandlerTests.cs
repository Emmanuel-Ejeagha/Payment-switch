using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Settlement.Application.DTOs;
using Settlement.Application.Features.Command.TriggerSettlement;
using Settlement.Application.Interfaces;
using Settlement.Domain.Entities;

namespace Settlement.Application.Tests.Handlers;

public class TriggerSettlementHandlerTests
{
    private readonly Mock<ISettlementBatchRepository> _repoMock = new();
    private readonly Mock<ILedgerService> _ledgerMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<TriggerSettlementCommand>> _validatorMock = new();
    private readonly TriggerSettlementHandler _handler;

    public TriggerSettlementHandlerTests()
    {
        _handler = new TriggerSettlementHandler(
            _repoMock.Object, _ledgerMock.Object,
            _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_NewBatch_ShouldCreateAndComplete()
    {
        var command = new TriggerSettlementCommand(new DateTime(2026, 7, 3));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByBatchDateAsync(command.BatchDate, It.IsAny<CancellationToken>())).ReturnsAsync((SettlementBatch?)null);
        _ledgerMock.Setup(l => l.GetDailyPayoutDataAsync(command.BatchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MerchantPayoutData>>.Success(new List<MerchantPayoutData>
            {
                new(Guid.NewGuid(), 1000m, 20m, "USD"),
                new(Guid.NewGuid(), 2000m, 40m, "USD")
            }));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _repoMock.Verify(r => r.AddAsync(It.Is<SettlementBatch>(b => b.BatchDate == command.BatchDate && b.Payouts.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingBatch_ShouldReturnExistingId()
    {
        var existingBatch = new SettlementBatch(Guid.NewGuid(), new DateTime(2026, 7, 3));
        var command = new TriggerSettlementCommand(existingBatch.BatchDate);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByBatchDateAsync(command.BatchDate, It.IsAny<CancellationToken>())).ReturnsAsync(existingBatch);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingBatch.Id, result.Value);
        _ledgerMock.Verify(l => l.GetDailyPayoutDataAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LedgerServiceFails_ShouldReturnError()
    {
        var command = new TriggerSettlementCommand(new DateTime(2026, 7, 3));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByBatchDateAsync(command.BatchDate, It.IsAny<CancellationToken>())).ReturnsAsync((SettlementBatch?)null);
        _ledgerMock.Setup(l => l.GetDailyPayoutDataAsync(command.BatchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MerchantPayoutData>>.Failure(new Error("Ledger.Error", "Connection failed")));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Ledger.Error", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationError()
    {
        var command = new TriggerSettlementCommand(default);
        SetupValidatorFailure(command, "BatchDate", "Batch date cannot be too far in the future.");

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "BatchDate");
    }

    private void SetupValidatorSuccess(TriggerSettlementCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupValidatorFailure(TriggerSettlementCommand command, string property, string error) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, error) }));
}