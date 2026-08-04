using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers;

public class CreatePaymentIntentHandlerTests
{
    private readonly Mock<IPaymentIntentRepository> _repoMock = new();
    private readonly Mock<IPaymentGatewayService> _gatewayMock = new();
    private readonly Mock<IMerchantService> _merchantServiceMock = new();
    private readonly Mock<ICardTokenRepository> _cardTokenRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly Mock<IValidator<CreatePaymentIntentCommand>> _validatorMock = new();
    private readonly Mock<ILogger<CreatePaymentIntentHandler>> _loggerMock = new();
    private readonly CreatePaymentIntentHandler _handler;

    public CreatePaymentIntentHandlerTests()
    {
        _handler = new CreatePaymentIntentHandler(_repoMock.Object, _gatewayMock.Object, _merchantServiceMock.Object, _cardTokenRepoMock.Object, _uowMock.Object, _dispatcherMock.Object, _validatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndCaptureIntent()
    {
        var command = new CreatePaymentIntentCommand(Guid.NewGuid(), 100, "USD", "Card", "1234", "Visa", "unique-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);
        SetupMerchantConfig(autoCapture: true);
        _gatewayMock.Setup(g => g.AuthorizeAsync(command.MerchantId, It.IsAny<Money>(), It.IsAny<CardDetails?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, "AUTH123", "GW-1", null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.IntentId);
        Assert.Equal("Captured", result.Value.Status);
        Assert.NotNull(result.Value.ClientSecret);
        _repoMock.Verify(r => r.AddAsync(It.Is<PaymentIntent>(i => i.IdempotencyKey.Value == command.IdempotencyKey), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ManualCaptureMerchant_ShouldAuthorizeOnly()
    {
        var command = new CreatePaymentIntentCommand(Guid.NewGuid(), 100, "USD", "Card", "1234", "Visa", "manual-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);
        SetupMerchantConfig(autoCapture: false);
        _gatewayMock.Setup(g => g.AuthorizeAsync(command.MerchantId, It.IsAny<Money>(), It.IsAny<CardDetails?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, "AUTH123", "GW-1", null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Authorized", result.Value.Status);
        Assert.Null(result.Value.ClientSecret);
    }

    [Fact]
    public async Task Handle_GatewayDeclines_ShouldFailIntent()
    {        var command = new CreatePaymentIntentCommand(Guid.NewGuid(), 100, "USD", "Card", "1234", "Visa", "declined-key");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);
        SetupMerchantConfig(autoCapture: true);
        _gatewayMock.Setup(g => g.AuthorizeAsync(command.MerchantId, It.IsAny<Money>(), It.IsAny<CardDetails?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Failure(new Error("Gateway.Declined", "Card declined.")));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("Failed", result.Value.Status);
        _repoMock.Verify(r => r.AddAsync(It.Is<PaymentIntent>(i => i.Status.Value == "Failed"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ShouldReturnExistingIntent()
    {
        var command = new CreatePaymentIntentCommand(Guid.NewGuid(), 100, "USD", "Card", "1234", "Visa", "dup-key");
        SetupValidatorSuccess(command);
        var existing = new PaymentIntent(Guid.NewGuid(), command.MerchantId, new Money(100, "USD"), new IdempotencyKey("dup-key"), PaymentMethod.Card);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value.IntentId);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<PaymentIntent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCardToken_ShouldResolveCardFromVault()
    {
        var merchantId = Guid.NewGuid();
        var command = new CreatePaymentIntentCommand(merchantId, 100, "USD", "Card", null, null, "token-key", "card_abc123");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);
        var vaultCard = new CardToken(merchantId, "card_abc123", "4242", "Visa", 12, 2030);
        _cardTokenRepoMock.Setup(r => r.GetByTokenAsync(merchantId, "card_abc123", It.IsAny<CancellationToken>())).ReturnsAsync(vaultCard);
        SetupMerchantConfig(autoCapture: true);
        _gatewayMock.Setup(g => g.AuthorizeAsync(merchantId, It.IsAny<Money>(), It.IsAny<CardDetails?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<GatewayResponse>.Success(new GatewayResponse(true, "AUTH123", "GW-1", null)));
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _handler.Handle(command);

        Assert.True(result.IsSuccess);
        _gatewayMock.Verify(g => g.AuthorizeAsync(merchantId, It.IsAny<Money>(), It.Is<CardDetails>(c => c.LastFour == "4242" && c.Brand == "Visa" && c.Token == "card_abc123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownCardToken_ShouldFail()
    {
        var command = new CreatePaymentIntentCommand(Guid.NewGuid(), 100, "USD", "Card", null, null, "bad-token-key", "card_doesnotexist");
        SetupValidatorSuccess(command);
        _repoMock.Setup(r => r.GetByIdempotencyKeyAsync(command.MerchantId, command.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);
        _cardTokenRepoMock.Setup(r => r.GetByTokenAsync(command.MerchantId, "card_doesnotexist", It.IsAny<CancellationToken>())).ReturnsAsync((CardToken?)null);

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Payment.InvalidCardToken");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<PaymentIntent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ShouldReturnValidationErrors()
    {        var command = new CreatePaymentIntentCommand(Guid.Empty, 0, "", "", null, null, "");
        SetupValidatorFailure(command, "Amount", "Amount must be greater than zero.");

        var result = await _handler.Handle(command);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Amount");
    }

    private void SetupValidatorSuccess(CreatePaymentIntentCommand command) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

    private void SetupMerchantConfig(bool autoCapture) =>
        _merchantServiceMock.Setup(m => m.GetMerchantConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MerchantConfig>.Success(new MerchantConfig("https://hook.test", autoCapture)));

    private void SetupValidatorFailure(CreatePaymentIntentCommand command, string property, string error) =>
        _validatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult(new[] { new ValidationFailure(property, error) }));
}