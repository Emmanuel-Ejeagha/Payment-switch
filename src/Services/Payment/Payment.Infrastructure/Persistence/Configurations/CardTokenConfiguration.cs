using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Configurations;

public class CardTokenConfiguration : IEntityTypeConfiguration<CardToken>
{
    public void Configure(EntityTypeBuilder<CardToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.MerchantId).IsRequired();
        builder.Property(t => t.Token).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Token);
        builder.Property(t => t.LastFour).IsRequired().HasMaxLength(4);
        builder.Property(t => t.Brand).IsRequired().HasMaxLength(50);
        builder.Property(t => t.ExpiryMonth).IsRequired();
        builder.Property(t => t.ExpiryYear).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
    }
}
