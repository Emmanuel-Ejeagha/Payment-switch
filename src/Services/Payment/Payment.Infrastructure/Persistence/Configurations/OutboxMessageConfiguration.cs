using Payment.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Payment.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.EventType).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.OccurredOn).IsRequired();
        builder.Property(m => m.Processed).IsRequired();
        builder.Property(m => m.CorrelationId).HasMaxLength(200);
        builder.Property(m => m.TraceParent).HasMaxLength(100);
        builder.Property(m => m.LeaseToken);
        builder.Property(m => m.LeaseExpiresAt);
        builder.HasIndex(m => new { m.Processed, m.LeaseExpiresAt });
    }
}