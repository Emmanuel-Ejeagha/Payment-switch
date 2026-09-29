using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.ValueObjects;
using NotificationPreferenceEntity = Notification.Domain.Entities.NotificationPreference;

namespace Notification.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreferenceEntity>
{
    public void Configure(EntityTypeBuilder<NotificationPreferenceEntity> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Recipient).IsRequired().HasMaxLength(255);
        builder.Property(p => p.Channel)
            .HasConversion(c => c.Value, c => NotificationChannel.FromString(c))
            .IsRequired();
        builder.Property(p => p.EventType).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Enabled).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
        builder.Ignore(p => p.DomainEvents);

        builder.HasIndex(p => new { p.Recipient, p.Channel, p.EventType }).IsUnique();
    }
}
