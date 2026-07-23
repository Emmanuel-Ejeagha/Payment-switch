using BuildingBlocks.Shared.Events;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers;

public class CreatePaymentIntentHandlerTests
{
    private readonly Mock<IPaymentIntentRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<CreatePaymentIntentCommand>> _validatorMock = new();
    private readonly Mock<ILogger<CreatePaymentIntentHandler>> _loggerMock = new();
    private readonly CreatePaymentIntentHandler _handler;

    public CreatePaymentIntentHandlerTests()
    {
        _handler = new CreatePaymentIntentHandler(_repoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateIntent()
    {
        var command = new CreatePaymentIntentCommand(Guid.NewGuid(), 100, "USD", "Card", "1234", "Visa", "unique-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.IntentId);
        Assert.Equal("Pending", result.Value.Status);
        _repoMock.Verify(r => r.AddAsync(It.Is<PaymentIntent>(i => i.IdempotencyKey.Value == command.IdempotencyKey), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ShouldFail()
    {
        var command = new CreatePaymentIntentCommand(Guid.NewGuid(), 100, "USD", "Card", "1234", "Visa", "dup-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync(new PaymentIntent(Guid.NewGuid(), command.MerchantId, new Money(100, "USD"), new IdempotencyKey("dup-key"), PaymentMethod.Card));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.IdempotencyKeyViolation", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {
        var command = new CreatePaymentIntentCommand(Guid.Empty, 0, "", "", null, null, "");
        SetupValidatorFailure(command, "Amount", "Amount must be greater than zero.");

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Amount");
    }

    private void SetupValidatorSuccess(CreatePaymentIntentCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupValidatorFailure(CreatePaymentIntentCommand command, string property, string error) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, error) }));
}