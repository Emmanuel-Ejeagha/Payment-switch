using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Shared.Retention;

/// <summary>
/// Copies a batch of expired rows into <see cref="ArchivedRecord"/> as JSON
/// snapshots, then removes the originals from the live table. Copy and delete run
/// in one transaction so a crash mid-batch never loses data from both sides.
/// </summary>
public static class DataArchiveWriter
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };

    public static async Task<int> ArchiveAsync<T>(
        DbContext db,
        DbSet<ArchivedRecord> archive,
        string sourceTable,
        string tableName,
        IQueryable<T> batch,
        Func<T, Guid> idSelector,
        CancellationToken cancellationToken) where T : class
    {
        var rows = await batch.AsNoTracking().ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        var ids = new Guid[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            ids[i] = idSelector(rows[i]);
            archive.Add(new ArchivedRecord(
                Guid.NewGuid(), sourceTable, JsonSerializer.Serialize(rows[i], SerializerOptions), now));
        }

        await db.SaveChangesAsync(cancellationToken);

        if (!IsSafeTableName(tableName))
            throw new ArgumentException($"Unsafe table name: {tableName}", nameof(tableName));

        // The table name is a hardcoded constant from our own EF mappings and is
        // guarded by IsSafeTableName; only the id list is parameterized.
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"DELETE FROM \"{tableName}\" WHERE \"Id\" = ANY({{0}})", new object[] { ids }, cancellationToken);
#pragma warning restore EF1002

        return rows.Count;
    }

    private static bool IsSafeTableName(string name) =>
        name.Length is > 0 and <= 64 && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
}