namespace Payment.Infrastructure.Services.Gateways;

public class PaystackMockGatewayProvider : MockPaymentGatewayProvider
{
    public PaystackMockGatewayProvider() : base("9999", "3000") { }

    public override string Name => "paystack";
}
