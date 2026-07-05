using Moq;
using Payment.Application.Features.Queries.GetPaymentIntentById;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Tests.Handlers.Queries;

public class GetPaymentIntentByIdHandlerTests
{
    [Fact]
    public async Task Handle_ExistingIntent_ReturnsDto()
    {
        var repoMock = new Mock<IPaymentIntentRepository>();
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(50, "USD"), new IdempotencyKey("key"), PaymentMethod.Card);
        repoMock.Setup(r => r.GetByIdAsync(intent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(intent);
        var handler = new GetPaymentIntentByIdHandler(repoMock.Object);

        var result = await handler.Handle(new GetPaymentIntentByIdQuery(intent.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value.Amount);
        Assert.Equal("Pending", result.Value.Status);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsError()
    {
        var repoMock = new Mock<IPaymentIntentRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((PaymentIntent?)null);
        var handler = new GetPaymentIntentByIdHandler(repoMock.Object);

        var result = await handler.Handle(new GetPaymentIntentByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.PaymentIntentNotFound", result.Errors[0].Code);
    }
}