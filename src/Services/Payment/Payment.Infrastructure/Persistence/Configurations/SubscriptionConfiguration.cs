using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.MerchantId).IsRequired();
        builder.Property(s => s.CustomerId).IsRequired();
        builder.Property(s => s.PlanId).IsRequired();

        builder.Property(s => s.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.Code).IsUnique();

        builder.Property(s => s.Status)
            .HasConversion(v => v.Value, v => SubscriptionStatus.FromString(v))
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.CardToken).IsRequired().HasMaxLength(100);

        builder.Property(s => s.CurrentPeriodStart).IsRequired();
        builder.Property(s => s.CurrentPeriodEnd).IsRequired();
        builder.Property(s => s.NextBillingAt);
        builder.Property(s => s.CancelAtPeriodEnd).IsRequired();
        builder.Property(s => s.CanceledAt);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt);

        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasIndex(s => s.MerchantId);
        builder.HasIndex(s => s.CustomerId);

        // Drives the billing worker's due query.
        builder.HasIndex(s => s.NextBillingAt);

        builder.Ignore(s => s.DomainEvents);
    }
}
