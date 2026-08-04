using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

public class PaymentLinkConfiguration : IEntityTypeConfiguration<PaymentLink>
{
    public void Configure(EntityTypeBuilder<PaymentLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.MerchantId).IsRequired();
        builder.Property(l => l.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(l => l.Code).IsUnique();
        builder.Property(l => l.Description).HasMaxLength(500);

        builder.OwnsOne(l => l.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
            a.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
        });

        builder.Property(l => l.Active).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();

        builder.Ignore(l => l.DomainEvents);
    }
}
