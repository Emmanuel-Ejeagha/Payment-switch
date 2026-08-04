namespace Payment.Infrastructure.Services.Gateways;

public class StripeMockGatewayProvider : MockPaymentGatewayProvider
{
    public StripeMockGatewayProvider() : base("7777") { }

    public override string Name => "stripe";
}
