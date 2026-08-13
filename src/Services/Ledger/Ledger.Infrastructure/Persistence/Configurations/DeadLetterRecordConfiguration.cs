using Ledger.Infrastructure.DeadLetter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Infrastructure.Persistence.Configurations;

public class DeadLetterRecordConfiguration : IEntityTypeConfiguration<DeadLetterRecord>
{
    public void Configure(EntityTypeBuilder<DeadLetterRecord> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.MessageId).IsRequired().HasMaxLength(100);
        builder.Property(d => d.EventType).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Queue).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Reason).IsRequired().HasMaxLength(100);
        builder.Property(d => d.RetryCount).IsRequired();
        builder.Property(d => d.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(d => d.ReceivedAt).IsRequired();
        builder.HasIndex(d => d.ReceivedAt);
    }
}