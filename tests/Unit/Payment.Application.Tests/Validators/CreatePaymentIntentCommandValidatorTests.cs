using Payment.Application.Features.Command.CreatePaymentIntent;

namespace Payment.Application.Tests.Validators;

public class CreatePaymentIntentCommandValidatorTests
{
    private readonly CreatePaymentIntentCommandValidator _validator = new();

    [Fact]
    public void ValidCardWithoutToken_Passes()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", "4242", "Visa", "idem-1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidCardWithToken_Passes()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", null, null, "idem-1", "card_abc1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidNonCardPaymentMethod_Passes()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "MobileMoney", null, null, "idem-1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidSecurityCode_Passes()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", "4242", "Visa", "idem-1", null, "123"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.Empty, 10000, "USD", "Card", "4242", "Visa", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void ZeroAmount_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 0, "USD", "Card", "4242", "Visa", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void InvalidCurrency_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "US", "Card", "4242", "Visa", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public void InvalidPaymentMethod_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Wallet", null, null, "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "PaymentMethod");
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", "4242", "Visa", ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void CardWithoutTokenEmptyCardLastFour_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", "", "Visa", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardLastFour");
    }

    [Fact]
    public void CardWithoutTokenShortCardLastFour_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", "42", "Visa", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardLastFour");
    }

    [Fact]
    public void CardWithoutTokenEmptyCardBrand_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", "4242", "", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardBrand");
    }

    [Fact]
    public void CardWithInvalidToken_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", null, null, "idem-1", "bad"));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardToken");
    }

    [Fact]
    public void InvalidSecurityCode_Fails()
    {
        var result = _validator.Validate(new CreatePaymentIntentCommand(Guid.NewGuid(), 10000, "USD", "Card", "4242", "Visa", "idem-1", null, "12"));

        Assert.Contains(result.Errors, e => e.PropertyName == "SecurityCode");
    }
}