using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MerchantId).IsRequired();
        builder.Property(p => p.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(500);

        builder.OwnsOne(p => p.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
            a.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
        });

        builder.OwnsOne(p => p.Interval, i =>
        {
            i.Property(v => v.Unit).HasColumnName("IntervalUnit").IsRequired().HasMaxLength(10);
            i.Property(v => v.Count).HasColumnName("IntervalCount").IsRequired();
        });

        builder.Property(p => p.Active).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt);

        builder.HasIndex(p => new { p.MerchantId, p.Active });

        builder.Ignore(p => p.DomainEvents);
    }
}
