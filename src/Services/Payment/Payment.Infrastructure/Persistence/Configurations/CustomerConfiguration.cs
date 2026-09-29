using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.MerchantId).IsRequired();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Email).IsRequired().HasMaxLength(320);
        builder.Property(c => c.Name).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(32);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Deleted).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt);

        // Email is unique per merchant, but only across live (non-deleted) customers so a
        // soft-deleted record never blocks re-creating the same customer.
        builder.HasIndex(c => new { c.MerchantId, c.Email })
               .IsUnique()
               .HasFilter("\"Deleted\" = false");

        builder.HasIndex(c => c.MerchantId);

        builder.Ignore(c => c.DomainEvents);
    }
}
