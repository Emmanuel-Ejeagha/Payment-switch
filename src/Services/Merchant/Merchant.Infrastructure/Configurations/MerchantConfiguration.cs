using Merchant.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Merchant.Infrastructure.Persistence.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<MerchantEntity>
{
    public void Configure(EntityTypeBuilder<MerchantEntity> builder)
    {
        builder.HasKey(m => m.Id);

        builder.OwnsOne(m => m.BusinessName, bn =>
        {
            bn.Property(b => b.Value).HasColumnName("BusinessName").IsRequired().HasMaxLength(200);
        });

        builder.OwnsOne(m => m.Email, e =>
        {
            e.Property(em => em.Value).HasColumnName("Email").IsRequired().HasMaxLength(255);
            e.HasIndex(em => em.Value).IsUnique();
        });

        builder.OwnsOne(m => m.WebhookUrl, wh =>
        {
            wh.Property(w => w.Value).HasColumnName("WebhookUrl").HasMaxLength(500);
        });

        builder.OwnsOne(m => m.Status, st =>
        {
            st.Property(s => s.Value).HasColumnName("Status").IsRequired().HasMaxLength(50);
        });

        builder.Property(m => m.EnabledPaymentMethods)
            .HasField("_paymentMethods")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt);

        builder.Ignore(m => m.DomainEvents);
    }
}