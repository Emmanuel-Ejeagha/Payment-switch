namespace Payment.Infrastructure.Services.Gateways;

public class StripeMockGatewayProvider : MockPaymentGatewayProvider
{
    public StripeMockGatewayProvider() : base("7777", "3001") { }

    public override string Name => "stripe";
}
