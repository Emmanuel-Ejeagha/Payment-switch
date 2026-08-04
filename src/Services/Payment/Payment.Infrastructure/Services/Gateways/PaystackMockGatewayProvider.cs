namespace Payment.Infrastructure.Services.Gateways;

public class PaystackMockGatewayProvider : MockPaymentGatewayProvider
{
    public PaystackMockGatewayProvider() : base("9999") { }

    public override string Name => "paystack";
}
