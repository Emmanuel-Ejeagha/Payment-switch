using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.MerchantId).IsRequired();
        builder.Property(i => i.CustomerId).IsRequired();
        builder.Property(i => i.SubscriptionId).IsRequired();

        builder.Property(i => i.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(i => i.Code).IsUnique();

        builder.OwnsOne(i => i.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
            a.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
        });

        builder.Property(i => i.Status)
            .HasConversion(v => v.Value, v => InvoiceStatus.FromString(v))
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.PeriodStart).IsRequired();
        builder.Property(i => i.PeriodEnd).IsRequired();
        builder.Property(i => i.PaymentIntentId);
        builder.Property(i => i.AttemptCount).IsRequired();
        builder.Property(i => i.LastError).HasMaxLength(1000);
        builder.Property(i => i.PaidAt);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt);

        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.HasIndex(i => i.MerchantId);

        // One invoice per subscription per billing period keeps worker retries idempotent.
        builder.HasIndex(i => new { i.SubscriptionId, i.PeriodStart }).IsUnique();

        builder.Ignore(i => i.DomainEvents);
    }
}
