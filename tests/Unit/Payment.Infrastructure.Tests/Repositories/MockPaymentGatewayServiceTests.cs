using Payment.Domain;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure.Tests.Services;

public class MockPaymentGatewayServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_ShouldReturnSuccessWithCodes()
    {
        var service = new MockPaymentGatewayService();
        var result = await service.AuthorizeAsync(Guid.NewGuid(), new Money(50, "USD"), null);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("AUTH-", result.Value!.AuthorizationCode);
        Assert.StartsWith("GW-", result.Value.GatewayReference);
    }
}