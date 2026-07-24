using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.AuthorizePayment;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers;

public class AuthorizePaymentHandlerTests
{
    private readonly Mock<IPaymentIntentRepository> _repoMock = new();
    private readonly Mock<IPaymentGatewayService> _gatewayMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<AuthorizePaymentCommand>> _validatorMock = new();
    private readonly Mock<IMerchantService> _merchantServiceMock = new();
    private readonly Mock<ILogger<AuthorizePaymentHandler>> _loggerMock = new();
    private readonly AuthorizePaymentHandler _handler;

    public AuthorizePaymentHandlerTests()
    {
        _handler = new AuthorizePaymentHandler(
        _repoMock.Object,
        _gatewayMock.Object,
        _uowMock.Object,
        _dispatcherMock.Object,
        _validatorMock.Object,
        _merchantServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_PendingIntent_ShouldAuthorize()
    {
        var intent = CreatePendingIntent();
        var command = new AuthorizePaymentCommand(intent.Id, null, null);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.AuthorizeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, "AUTH123", "GW-1", null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _merchantServiceMock
        .Setup(m => m.GetMerchantStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result<string>.Success("active"));

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("AUTH123", result.Value.AuthorizationCode);
        Assert.Equal("GW-1", result.Value.GatewayReference);
        Assert.Equal("Authorized", result.Value.Status);
    }

    [Fact]
    public async Task Handle_IntentNotFound_ShouldFail()
    {
        var command = new AuthorizePaymentCommand(Guid.NewGuid(), null, null);
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.PaymentIntentNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_GatewayFails_ShouldFail()
    {
        var intent = CreatePendingIntent();
        var command = new AuthorizePaymentCommand(intent.Id, null, null);
        SetupValidatorSuccess(command);
        _merchantServiceMock
    .Setup(m => m.GetMerchantStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(Result<string>.Success("active"));
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.AuthorizeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Failure(new Error("Gateway.Declined", "Card declined.")));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.AuthorizationFailed", result.Errors[0].Code);
    }

    private PaymentIntent CreatePendingIntent() =>
        new(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("k"), PaymentMethod.Card);

    private void SetupValidatorSuccess(AuthorizePaymentCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}
