using Merchant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Merchant.Infrastructure.Persistence.Configurations;

public class MerchantApiKeyConfiguration : IEntityTypeConfiguration<MerchantApiKey>
{
    public void Configure(EntityTypeBuilder<MerchantApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever();

        builder.Property(k => k.KeyHash).IsRequired().HasMaxLength(128);
        builder.Property(k => k.KeyPrefix).IsRequired().HasMaxLength(16);
        builder.Property(k => k.Environment).IsRequired().HasMaxLength(10);
        builder.Property(k => k.CreatedAt).IsRequired();
        builder.Property(k => k.RevokedAt);

        builder.HasIndex(k => new { k.KeyHash, k.KeyPrefix }).IsUnique();

        builder.HasOne<MerchantEntity>()
            .WithMany(m => m.ApiKeys)
            .HasForeignKey(k => k.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
