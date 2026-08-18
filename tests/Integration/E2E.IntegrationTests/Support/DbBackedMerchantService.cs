using BuildingBlocks.Shared.Results;
using BuildingBlocks.Shared.Security;
using MerchantAppDbContext = Merchant.Infrastructure.Persistence.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace E2E.IntegrationTests.Support;

/// <summary>
/// In-memory stand-in for the Payment API's gRPC merchant lookup. Reads the
/// merchant and API keys straight from the Merchant database so the E2E flow
/// exercises the real stored state (status transitions, key hashes) without
/// standing up a gRPC server.
/// </summary>
public sealed class DbBackedMerchantService : IMerchantService
{
    private readonly DbContextOptions<MerchantAppDbContext> _options;

    public DbBackedMerchantService(string connectionString)
    {
        _options = new DbContextOptionsBuilder<MerchantAppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public async Task<Result<string>> GetMerchantStatusAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        await using var db = new MerchantAppDbContext(_options);
        var merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.Id == merchantId, cancellationToken);
        if (merchant is null)
            return new Error("Merchant.NotFound", "Merchant not found.");

        return Result<string>.Success(merchant.Status.Value);
    }

    public async Task<Result<MerchantConfig>> GetMerchantConfigAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        await using var db = new MerchantAppDbContext(_options);
        var merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.Id == merchantId, cancellationToken);
        if (merchant is null)
            return new Error("Merchant.NotFound", "Merchant not found.");

        return Result<MerchantConfig>.Success(new MerchantConfig(
            merchant.WebhookUrl?.Value,
            merchant.AutoCapture,
            merchant.WebhookSecret?.Value,
            merchant.PreviousWebhookSecret?.Value,
            merchant.WebhookSecretRotatedAtUtc));
    }

    public async Task<Result<Guid?>> GetMerchantOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        await using var db = new MerchantAppDbContext(_options);
        var merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.Id == merchantId, cancellationToken);
        if (merchant is null)
            return new Error("Merchant.NotFound", "Merchant not found.");

        return Result<Guid?>.Success(merchant.OwnerId);
    }

    public async Task<Result<MerchantKeyResolution>> ResolveApiKeyAsync(string keyPrefix, string keyValue, CancellationToken cancellationToken = default)
    {
        await using var db = new MerchantAppDbContext(_options);
        var candidates = await (from key in db.MerchantApiKeys.AsNoTracking()
                                join merchant in db.Merchants.AsNoTracking() on key.MerchantId equals merchant.Id
                                where key.KeyPrefix == keyPrefix && key.RevokedAt == null
                                select new { Key = key, Merchant = merchant }).ToListAsync(cancellationToken);

        var match = candidates.FirstOrDefault(c => ApiKeyHasher.Verify(keyValue, c.Key.KeyHash));
        if (match is null)
            return new Error("Payment.InvalidApiKey", "Invalid API key.");

        return Result<MerchantKeyResolution>.Success(
            new MerchantKeyResolution(match.Merchant.Id, match.Merchant.Status.Value, match.Key.Environment));
    }
}