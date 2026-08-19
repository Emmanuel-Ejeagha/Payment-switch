using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;
using PaymentSwitch.IntegrationTests.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Notification.API.IntegrationTests;

public class SignalRHardeningTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public SignalRHardeningTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OversizedInvocation_ClosesConnection()
    {
        var connection = CreateConnection();
        var closed = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Closed += ex =>
        {
            closed.TrySetResult(ex);
            return Task.CompletedTask;
        };

        await connection.StartAsync();

        var oversized = new string('x', 64 * 1024);
        await Assert.ThrowsAnyAsync<Exception>(() => connection.InvokeAsync<object>("Send", oversized));
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(15));

        await connection.DisposeAsync();
    }

    private HubConnection CreateConnection()
    {
        var server = _factory.Server;
        var url = new Uri(server.BaseAddress, "/hubs/payment-notifications");
        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(CreateAdminToken());
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();
    }

    private static string CreateAdminToken()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "admin@paymentswitch.com"),
            new(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecrets.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "NotificationService",
            audience: TestSecrets.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}