using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.RefundPayment;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers;

public class RefundPaymentHandlerTests
{
    private readonly Mock<IPaymentIntentRepository> _repoMock = new();
    private readonly Mock<IPaymentGatewayService> _gatewayMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<RefundPaymentCommand>> _validatorMock = new();
    private readonly RefundPaymentHandler _handler;

    public RefundPaymentHandlerTests()
    {
        _handler = new RefundPaymentHandler(_repoMock.Object, _gatewayMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_CapturedIntent_ShouldRefund()
    {
        var intent = CreateCapturedIntent();
        var command = new RefundPaymentCommand(intent.Id, null); // full refund
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.RefundAsync(intent.MerchantId, intent.GatewayReference!, It.IsAny<Money>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, null, "GW-REF", null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("FullyRefunded", result.Value.Status);
    }

    [Fact]
    public async Task Handle_GatewayFails_ShouldFail()
    {
        var intent = CreateCapturedIntent();
        var command = new RefundPaymentCommand(intent.Id, 50);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.RefundAsync(intent.MerchantId, intent.GatewayReference!, It.IsAny<Money>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Failure(new Error("Gateway.Error", "Refund failed")));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.RefundFailed", result.Errors[0].Code);
    }

    private PaymentIntent CreateCapturedIntent()
    {
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("k"), PaymentMethod.Card);
        intent.Authorize(new AuthorizationCode("AUTH"), new GatewayReference("GW"));
        intent.Capture(new Money(100, "USD"));
        intent.ClearDomainEvents();
        return intent;
    }

    private void SetupValidatorSuccess(RefundPaymentCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}