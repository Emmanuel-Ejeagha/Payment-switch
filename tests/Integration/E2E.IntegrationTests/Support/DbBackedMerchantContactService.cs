using MerchantAppDbContext = Merchant.Infrastructure.Persistence.AppDbContext;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;

namespace E2E.IntegrationTests.Support;

/// <summary>
/// In-memory stand-in for the Notification API's gRPC merchant lookup. Resolves
/// the merchant's contact email from the Merchant database so notifications for
/// payment events carry the real owner address.
/// </summary>
public sealed class DbBackedMerchantContactService : IMerchantContactService
{
    private readonly DbContextOptions<MerchantAppDbContext> _options;

    public DbBackedMerchantContactService(string connectionString)
    {
        _options = new DbContextOptionsBuilder<MerchantAppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public async Task<string?> GetMerchantEmailAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        await using var db = new MerchantAppDbContext(_options);
        var merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.Id == merchantId, cancellationToken);
        return merchant?.Email.Value;
    }
}