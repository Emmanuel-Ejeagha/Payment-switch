using Payment.Application.Features.Command.AuthorizePayment;

namespace Payment.Application.Tests.Validators;

public class AuthorizePaymentCommandValidatorTests
{
    private readonly AuthorizePaymentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new AuthorizePaymentCommand(Guid.NewGuid(), "4242", "Visa", "idem-key-123"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NullIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new AuthorizePaymentCommand(Guid.NewGuid(), "4242", "Visa", null));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new AuthorizePaymentCommand(Guid.NewGuid(), "4242", "Visa", ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void InvalidCharactersIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new AuthorizePaymentCommand(Guid.NewGuid(), "4242", "Visa", "bad:key!"));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void EmptyIntentId_Fails()
    {
        var result = _validator.Validate(new AuthorizePaymentCommand(Guid.Empty, "4242", "Visa", "idem-key-123"));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntentId");
    }

    [Fact]
    public void OverMaxLengthIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new AuthorizePaymentCommand(Guid.NewGuid(), "4242", "Visa", new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }
}