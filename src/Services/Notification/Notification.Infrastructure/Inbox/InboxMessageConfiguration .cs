using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notification.Infrastructure.Inbox;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MessageId).IsRequired().HasMaxLength(100);
        builder.HasIndex(m => m.MessageId).IsUnique();
        builder.Property(m => m.EventType).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.OccurredOn).IsRequired();
        builder.Property(m => m.ProcessedAt);
    }
}