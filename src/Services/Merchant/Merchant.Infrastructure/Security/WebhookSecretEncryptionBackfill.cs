using Merchant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Merchant.Infrastructure.Security;

/// <summary>
/// One-time re-encryption of legacy plaintext webhook secrets at startup.
/// Reads the raw column values (bypassing the EF value converter) and only
/// touches rows that are not already <c>enc:</c>-prefixed, so it is idempotent.
/// </summary>
public class WebhookSecretEncryptionBackfill
{
    private readonly AppDbContext _context;

    public WebhookSecretEncryptionBackfill(AppDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Id\", \"WebhookSecret\" FROM \"Merchants\" WHERE \"WebhookSecret\" IS NOT NULL";

        var rows = new List<(Guid Id, string Secret)>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                rows.Add((reader.GetGuid(0), reader.GetString(1)));
        }

        foreach (var (id, secret) in rows.Where(r => !WebhookSecretEncryptor.IsEncrypted(r.Secret)))
        {
            var encrypted = WebhookSecretEncryptor.Encrypt(secret);
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Merchants\" SET \"WebhookSecret\" = {encrypted} WHERE \"Id\" = {id}",
                cancellationToken);
        }
    }
}
