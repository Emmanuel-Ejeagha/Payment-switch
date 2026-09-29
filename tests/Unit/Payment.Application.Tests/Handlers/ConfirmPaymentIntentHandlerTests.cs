using BuildingBlocks.Shared.Exceptions;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.ConfirmPaymentIntent;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers;

public class ConfirmPaymentIntentHandlerTests
{
    private readonly Mock<IPaymentIntentRepository> _repoMock = new();
    private readonly Mock<IPaymentGatewayService> _gatewayMock = new();
    private readonly Mock<IMerchantService> _merchantServiceMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IValidator<ConfirmPaymentIntentCommand>> _validatorMock = new();
    private readonly Mock<ILogger<ConfirmPaymentIntentHandler>> _loggerMock = new();
    private readonly ConfirmPaymentIntentHandler _handler;

    public ConfirmPaymentIntentHandlerTests()
    {
        _handler = new ConfirmPaymentIntentHandler(
            _repoMock.Object,
            _gatewayMock.Object,
            _merchantServiceMock.Object,
            _uowMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_RequiresActionIntent_ShouldConfirmAndAuthorize()
    {
        var intent = CreateRequiresActionIntent();
        var command = new ConfirmPaymentIntentCommand(intent.MerchantId, intent.Id, "confirm-key");
        SetupValidatorSuccess(command);
        SetupMerchantConfig(autoCapture: false);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.ConfirmChallengeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, intent.GatewayReference!.Value, It.IsAny<CardSecurityCode?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, "AUTH-3DS", intent.GatewayReference!.Value, null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Authorized", result.Value!.Status);
        Assert.Equal(intent.Id, result.Value!.IntentId);
    }

    [Fact]
    public async Task Handle_RequiresActionIntent_AutoCapture_ShouldCapture()
    {
        var intent = CreateRequiresActionIntent();
        var command = new ConfirmPaymentIntentCommand(intent.MerchantId, intent.Id, "confirm-key");
        SetupValidatorSuccess(command);
        SetupMerchantConfig(autoCapture: true);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.ConfirmChallengeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, intent.GatewayReference!.Value, It.IsAny<CardSecurityCode?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, "AUTH-3DS", intent.GatewayReference!.Value, null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Captured", result.Value!.Status);
        Assert.NotNull(result.Value!.ClientSecret);
    }

    [Fact]
    public async Task Handle_IntentNotFound_ShouldFail()
    {
        var command = new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.NewGuid(), "confirm-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.PaymentIntentNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_IntentOfAnotherMerchant_ShouldFail()
    {
        var intent = CreateRequiresActionIntent();
        var command = new ConfirmPaymentIntentCommand(Guid.NewGuid(), intent.Id, "confirm-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.PaymentIntentNotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_NotRequiresAction_ShouldFailWithInvalidStatusTransition()
    {
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("k"), PaymentMethod.Card);
        var command = new ConfirmPaymentIntentCommand(intent.MerchantId, intent.Id, "confirm-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidStatusTransition", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_GatewayChallengeFails_ShouldFail()
    {
        var intent = CreateRequiresActionIntent();
        var command = new ConfirmPaymentIntentCommand(intent.MerchantId, intent.Id, "confirm-key");
        SetupValidatorSuccess(command);
        SetupMerchantConfig(autoCapture: false);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.ConfirmChallengeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, intent.GatewayReference!.Value, It.IsAny<CardSecurityCode?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Failure(new Error("Gateway.Declined", "Challenge failed.")));

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.ChallengeConfirmationFailed", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflict_ShouldFail()
    {
        var intent = CreateRequiresActionIntent();
        var command = new ConfirmPaymentIntentCommand(intent.MerchantId, intent.Id, "confirm-key");
        SetupValidatorSuccess(command);
        SetupMerchantConfig(autoCapture: false);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        _gatewayMock.Setup(g => g.ConfirmChallengeAsync(intent.MerchantId, intent.Amount, intent.CardDetails, intent.GatewayReference!.Value, It.IsAny<CardSecurityCode?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, "AUTH-3DS", intent.GatewayReference!.Value, null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ConcurrencyConflictException());

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.ConcurrencyConflict", result.Errors[0].Code);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedWithSameKey_ReturnsOriginalResult()
    {
        var intent = CreateRequiresActionIntent();
        intent.ConfirmAction(new AuthorizationCode("AUTH-3DS"), intent.GatewayReference!, "confirm-key");
        intent.ClearDomainEvents();
        var command = new ConfirmPaymentIntentCommand(intent.MerchantId, intent.Id, "confirm-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(intent.Id, result.Value!.IntentId);
        Assert.Equal("Authorized", result.Value!.Status);
        _gatewayMock.Verify(
            g => g.ConfirmChallengeAsync(It.IsAny<Guid>(), It.IsAny<Money>(), It.IsAny<CardDetails?>(), It.IsAny<string>(), It.IsAny<CardSecurityCode?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmedWithDifferentKey_StillFails()
    {
        var intent = CreateRequiresActionIntent();
        intent.ConfirmAction(new AuthorizationCode("AUTH-3DS"), intent.GatewayReference!, "original-key");
        intent.ClearDomainEvents();
        var command = new ConfirmPaymentIntentCommand(intent.MerchantId, intent.Id, "other-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.InvalidStatusTransition", result.Errors[0].Code);
    }

    private PaymentIntent CreateRequiresActionIntent()
    {
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("k"), PaymentMethod.Card);
        intent.RequireAction(new GatewayReference("GTW-3DS"));
        intent.ClearDomainEvents();
        return intent;
    }

    private void SetupMerchantConfig(bool autoCapture) =>
        _merchantServiceMock.Setup(m => m.GetMerchantConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MerchantConfig>.Success(new MerchantConfig("https://hook.test", autoCapture)));

    private void SetupValidatorSuccess(ConfirmPaymentIntentCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());
}
