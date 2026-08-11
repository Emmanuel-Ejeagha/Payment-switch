using Merchant.Domain.ValueObjects;
using Merchant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MerchantEntity = Merchant.Domain.Entities.Merchant;

namespace Merchant.API.IntegrationTests;

/// <summary>
/// TASK-006: webhook secrets are encrypted at rest and never stored or
/// returned in plaintext through the REST layer.
/// </summary>
public class WebhookSecretTests : IClassFixture<MerchantApiFactory>
{
    private readonly MerchantApiFactory _factory;

    public WebhookSecretTests(MerchantApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WebhookSecret_IsEncryptedAtRest()
    {
        var merchantId = Guid.NewGuid();
        string plaintext;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var merchant = new MerchantEntity(merchantId, new BusinessName("Secret Corp"), new MerchantEmail($"secret-{Guid.NewGuid()}@example.com"));
            plaintext = merchant.WebhookSecret!.Value;
            db.Merchants.Add(merchant);
            await db.SaveChangesAsync();
        }

        var raw = await ReadRawColumnAsync(merchantId, "WebhookSecret");

        Assert.NotNull(raw);
        Assert.StartsWith("enc:", raw);
        Assert.DoesNotContain(plaintext, raw);
    }

    [Fact]
    public async Task Rotation_StoresEncryptedPreviousSecret()
    {
        var merchantId = Guid.NewGuid();
        string? previous = null;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Merchants.Add(new MerchantEntity(merchantId, new BusinessName("Secret Corp"), new MerchantEmail($"secret-{Guid.NewGuid()}@example.com")));
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var merchant = await db.Merchants.FindAsync(merchantId);
            Assert.NotNull(merchant);
            merchant.RotateWebhookSecret();
            previous = merchant.PreviousWebhookSecret!.Value;
            await db.SaveChangesAsync();
        }

        var rawCurrent = await ReadRawColumnAsync(merchantId, "WebhookSecret");
        var rawPrevious = await ReadRawColumnAsync(merchantId, "PreviousWebhookSecret");
        var rotatedAt = await ReadRawColumnAsync(merchantId, "WebhookSecretRotatedAtUtc");

        Assert.NotNull(previous);
        Assert.StartsWith("enc:", rawCurrent);
        Assert.StartsWith("enc:", rawPrevious);
        Assert.DoesNotContain(previous, rawPrevious);
        Assert.False(string.IsNullOrEmpty(rotatedAt));
    }

    private async Task<string?> ReadRawColumnAsync(Guid merchantId, string column)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT \"{column}\" FROM \"Merchants\" WHERE \"Id\" = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = merchantId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return result is DBNull ? null : result?.ToString();
    }
}
