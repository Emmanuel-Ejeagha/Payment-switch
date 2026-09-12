using BuildingBlocks.Shared.Exceptions;
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
    private readonly Mock<IValidator<TriggerSettlementCommand>> _validatorMock = new();
    private readonly Mock<ILogger<TriggerSettlementHandler>> _loggerMock = new();
    private readonly TriggerSettlementHandler _handler;

    public TriggerSettlementHandlerTests()
    {
        _handler = new TriggerSettlementHandler(
            _repoMock.Object, _ledgerMock.Object,
            _uowMock.Object, _validatorMock.Object, _loggerMock.Object);
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
                new(Guid.NewGuid(), 1000L, 20L, "USD"),
                new(Guid.NewGuid(), 2000L, 40L, "USD")
            }));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.Id);
        _repoMock.Verify(r => r.AddAsync(It.Is<SettlementBatch>(b => b.BatchDate == command.BatchDate && b.Payouts.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RefundHeavyDay_ShouldCompleteWithZeroNetPayout()
    {
        var command = new TriggerSettlementCommand(new DateTime(2026, 7, 4));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByBatchDateAsync(command.BatchDate, It.IsAny<CancellationToken>())).ReturnsAsync((SettlementBatch?)null);
        _ledgerMock.Setup(l => l.GetDailyPayoutDataAsync(command.BatchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MerchantPayoutData>>.Success(new List<MerchantPayoutData>
            {
                new(Guid.NewGuid(), 0L, 150L, "USD")
            }));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.AddAsync(It.Is<SettlementBatch>(b => b.Payouts.Count == 1 && b.Payouts[0].NetAmount.Amount == 0L), It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.Equal(existingBatch.Id, result.Value!.Id);
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

    [Fact]
    public async Task Handle_ConcurrencyConflict_ShouldFail()
    {
        var command = new TriggerSettlementCommand(new DateTime(2026, 7, 3));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByBatchDateAsync(command.BatchDate, It.IsAny<CancellationToken>())).ReturnsAsync((SettlementBatch?)null);
        _ledgerMock.Setup(l => l.GetDailyPayoutDataAsync(command.BatchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MerchantPayoutData>>.Success(new List<MerchantPayoutData>
            {
                new(Guid.NewGuid(), 1000L, 20L, "USD")
            }));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Settlement.ConcurrencyConflict", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_TieOutMismatch_ShouldFailWithoutCompleting()
    {
        var command = new TriggerSettlementCommand(new DateTime(2026, 7, 3));
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByBatchDateAsync(command.BatchDate, It.IsAny<CancellationToken>())).ReturnsAsync((SettlementBatch?)null);
        _ledgerMock.SetupSequence(l => l.GetDailyPayoutDataAsync(command.BatchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MerchantPayoutData>>.Success(new List<MerchantPayoutData>
            {
                new(Guid.NewGuid(), 1000L, 20L, "USD")
            }))
            .ReturnsAsync(Result<List<MerchantPayoutData>>.Success(new List<MerchantPayoutData>
            {
                new(Guid.NewGuid(), 2000L, 40L, "USD")
            }));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Settlement.LedgerTieOutMismatch", result.Errors[0].Code);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UniqueViolationOnSave_ShouldReturnExistingBatch()
    {
        var existingBatch = new SettlementBatch(Guid.NewGuid(), new DateTime(2026, 7, 3));
        var command = new TriggerSettlementCommand(new DateTime(2026, 7, 3));
        SetupValidatorSuccess(command);
        _repoMock.SetupSequence(r => r.GetByBatchDateAsync(command.BatchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementBatch?)null)
            .ReturnsAsync(existingBatch);
        _ledgerMock.Setup(l => l.GetDailyPayoutDataAsync(command.BatchDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<MerchantPayoutData>>.Success(new List<MerchantPayoutData>
            {
                new(Guid.NewGuid(), 1000L, 20L, "USD")
            }));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException());

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingBatch.Id, result.Value!.Id);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<SettlementBatch>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupValidatorSuccess(TriggerSettlementCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupValidatorFailure(TriggerSettlementCommand command, string property, string error) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, error) }));
}