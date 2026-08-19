using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Shared.Retention;

/// <summary>
/// Shared EF mapping for <see cref="ArchivedRecord"/> so the archive table is
/// identical across every service. Each service's DbContext calls
/// <see cref="ConfigureArchivedRecord"/> from its model configuration.
/// </summary>
public static class RetentionModelConfiguration
{
    public static void ConfigureArchivedRecord(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ArchivedRecord>();
        entity.ToTable("ArchivedRecords");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).ValueGeneratedNever();
        entity.Property(r => r.SourceTable).IsRequired().HasMaxLength(64);
        entity.Property(r => r.Payload).IsRequired().HasColumnType("jsonb");
        entity.Property(r => r.ArchivedAt).IsRequired();
        entity.HasIndex(r => r.ArchivedAt);
    }
}