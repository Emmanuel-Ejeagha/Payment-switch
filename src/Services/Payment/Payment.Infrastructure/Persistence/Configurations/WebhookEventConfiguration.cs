using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MerchantId).IsRequired();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Payload).IsRequired();
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.HasIndex(e => new { e.Status, e.NextAttemptAt });
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Ignore(e => e.DomainEvents);
    }
}
