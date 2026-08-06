using BuildingBlocks.Shared.Results;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Payment.Application.Features.Command.CheckoutTokenize;
using Payment.Application.Features.Command.CreatePaymentLink;
using Payment.Application.Features.Queries.GetPaymentLinkByCode;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers.Queries;

public class PaymentLinkHandlerTests
{
    [Fact]
    public async Task CreatePaymentLink_ShouldGenerateCodeAndReturnResponse()
    {
        var repoMock = new Mock<IPaymentLinkRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var validatorMock = new Mock<IValidator<CreatePaymentLinkCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreatePaymentLinkCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        var loggerMock = new Mock<ILogger<CreatePaymentLinkHandler>>();
        var handler = new CreatePaymentLinkHandler(repoMock.Object, uowMock.Object, validatorMock.Object, loggerMock.Object);

        var result = await handler.Handle(new CreatePaymentLinkCommand(Guid.NewGuid(), 500, "USD", "Test link"));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("pl_", result.Value!.Code);
        Assert.Equal(500, result.Value!.Amount);
        Assert.True(result.Value!.Active);
        repoMock.Verify(r => r.AddAsync(It.IsAny<PaymentLink>(), It.IsAny<CancellationToken>()), Times.Once);
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentLink_InvalidAmount_ReturnsValidationError()
    {
        var validatorMock = new Mock<IValidator<CreatePaymentLinkCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreatePaymentLinkCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Amount", "Amount must be greater than zero.") }));
        var handler = new CreatePaymentLinkHandler(new Mock<IPaymentLinkRepository>().Object, new Mock<IUnitOfWork>().Object, validatorMock.Object, new Mock<ILogger<CreatePaymentLinkHandler>>().Object);

        var result = await handler.Handle(new CreatePaymentLinkCommand(Guid.NewGuid(), 0, "USD", "Test"));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == "Amount");
    }

    [Fact]
    public async Task GetPaymentLinkByCode_ExistingActiveLink_ReturnsDetails()
    {
        var repoMock = new Mock<IPaymentLinkRepository>();
        var link = new PaymentLink(Guid.NewGuid(), Guid.NewGuid(), new Money(2500, "NGN"), "pl_abc123", "Gift");
        repoMock.Setup(r => r.GetByCodeAsync("pl_abc123", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var handler = new GetPaymentLinkByCodeHandler(repoMock.Object);

        var result = await handler.Handle(new GetPaymentLinkByCodeQuery("pl_abc123"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2500, result.Value!.Amount);
        Assert.Equal("NGN", result.Value!.Currency);
        Assert.True(result.Value!.Active);
    }

    [Fact]
    public async Task GetPaymentLinkByCode_NotFound_ReturnsError()
    {
        var repoMock = new Mock<IPaymentLinkRepository>();
        repoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PaymentLink?)null);
        var handler = new GetPaymentLinkByCodeHandler(repoMock.Object);

        var result = await handler.Handle(new GetPaymentLinkByCodeQuery("pl_missing"));

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentLink.NotFound", result.Errors[0].Code);
    }

    [Fact]
    public async Task GetPaymentLinkByCode_Inactive_ReturnsError()
    {
        var repoMock = new Mock<IPaymentLinkRepository>();
        var link = new PaymentLink(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), "pl_off", "Off");
        link.Deactivate();
        repoMock.Setup(r => r.GetByCodeAsync("pl_off", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        var handler = new GetPaymentLinkByCodeHandler(repoMock.Object);

        var result = await handler.Handle(new GetPaymentLinkByCodeQuery("pl_off"));

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentLink.Inactive", result.Errors[0].Code);
    }

    [Fact]
    public async Task CheckoutTokenize_ValidCard_CreatesTokenForLinkMerchant()
    {
        var linkRepoMock = new Mock<IPaymentLinkRepository>();
        var merchantId = Guid.NewGuid();
        var link = new PaymentLink(Guid.NewGuid(), merchantId, new Money(100, "USD"), "pl_tokenize", "T");
        linkRepoMock.Setup(r => r.GetByCodeAsync("pl_tokenize", It.IsAny<CancellationToken>())).ReturnsAsync(link);

        var cardTokenRepoMock = new Mock<ICardTokenRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var validatorMock = new Mock<IValidator<CheckoutTokenizeCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CheckoutTokenizeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        var loggerMock = new Mock<ILogger<CheckoutTokenizeHandler>>();
        var handler = new CheckoutTokenizeHandler(linkRepoMock.Object, cardTokenRepoMock.Object, uowMock.Object, validatorMock.Object, loggerMock.Object);

        var result = await handler.Handle(new CheckoutTokenizeCommand("pl_tokenize", "4242424242424242", 12, 2030));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("card_", result.Value!.Token);
        Assert.Equal("4242", result.Value!.LastFour);
        Assert.Equal("Visa", result.Value!.Brand);
        cardTokenRepoMock.Verify(r => r.AddAsync(It.Is<CardToken>(t => t.MerchantId == merchantId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckoutTokenize_InvalidLuhn_ReturnsError()
    {
        var linkRepoMock = new Mock<IPaymentLinkRepository>();
        var cardTokenRepoMock = new Mock<ICardTokenRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var validatorMock = new Mock<IValidator<CheckoutTokenizeCommand>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<CheckoutTokenizeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("CardNumber", "Card number failed Luhn validation.") }));
        var handler = new CheckoutTokenizeHandler(linkRepoMock.Object, cardTokenRepoMock.Object, uowMock.Object, validatorMock.Object, new Mock<ILogger<CheckoutTokenizeHandler>>().Object);

        var result = await handler.Handle(new CheckoutTokenizeCommand("pl_any", "123456", 12, 2030));

        Assert.True(result.IsFailure);
        cardTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<CardToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
