using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.ValueObjects;
using NotificationEntity = Notification.Domain.Entities.Notification;


namespace Notification.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Recipient).IsRequired().HasMaxLength(255);
        builder.Property(n => n.Channel)
            .HasConversion(c => c.Value, c => NotificationChannel.FromString(c))
            .IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(500);
        builder.Property(n => n.Body);
        builder.Property(n => n.WebhookUrl).HasMaxLength(500);
        builder.Property(n => n.Payload).IsRequired().HasColumnType("jsonb");
        builder.Property(n => n.Status)
            .HasConversion(s => s.Value, s => NotificationStatus.FromString(s))
            .IsRequired();
        builder.Property(n => n.RetryCount).IsRequired();
        builder.Property(n => n.MaxRetries).IsRequired();
        builder.Property(n => n.NextRetryAt);
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.ProcessedAt);
        builder.Ignore(n => n.DomainEvents);
    }
}