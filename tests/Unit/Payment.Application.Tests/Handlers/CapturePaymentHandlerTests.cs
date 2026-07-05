using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CapturePayment;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers;

public class CapturePaymentHandlerTests
{
    private readonly Mock<IPaymentIntentRepository> _repoMock = new();
    private readonly Mock<IPaymentGatewayService> _gatewayMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<CapturePaymentCommand>> _validatorMock = new();
    private readonly CapturePaymentHandler _handler;

    public CapturePaymentHandlerTests()
    {
        _handler = new CapturePaymentHandler(_repoMock.Object, _gatewayMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task Handle_AuthorizedIntent_ShouldCapture()
    {
        var intent = CreateAuthorizedIntent();
        var command = new CapturePaymentCommand(intent.Id, null); // full capture
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.CaptureAsync(intent.MerchantId, intent.GatewayReference!, It.IsAny<Money>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, null, "GW-CAP", null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Captured", result.Value.Status);
    }

    [Fact]
    public async Task Handle_GatewayFails_ShouldFail()
    {
        var intent = CreateAuthorizedIntent();
        var command = new CapturePaymentCommand(intent.Id, 50);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.CaptureAsync(intent.MerchantId, intent.GatewayReference!, It.IsAny<Money>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Failure(new Error("Gateway.Error", "Capture failed")));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.CaptureFailed", result.Errors[0].Code);
    }

    private PaymentIntent CreateAuthorizedIntent()
    {
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("k"), PaymentMethod.Card);
        intent.Authorize(new AuthorizationCode("AUTH"), new GatewayReference("GW"));
        intent.ClearDomainEvents();
        return intent;
    }

    private void SetupValidatorSuccess(CapturePaymentCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}