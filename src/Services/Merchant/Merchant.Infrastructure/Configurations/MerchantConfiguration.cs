using Merchant.Domain.Entities;
using Merchant.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Merchant.Infrastructure.Persistence.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<MerchantEntity>
{
    public void Configure(EntityTypeBuilder<MerchantEntity> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.OwnerId);

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

        builder.OwnsOne(m => m.WebhookSecret, ws =>
        {
            ws.Property(w => w.Value)
              .HasColumnName("WebhookSecret")
              .HasMaxLength(256)
              .HasConversion(
                  v => WebhookSecretEncryptor.Encrypt(v),
                  v => WebhookSecretEncryptor.Decrypt(v));
        });

        builder.OwnsOne(m => m.PreviousWebhookSecret, ws =>
        {
            ws.Property(w => w.Value)
              .HasColumnName("PreviousWebhookSecret")
              .HasMaxLength(256)
              .HasConversion(
                  v => WebhookSecretEncryptor.Encrypt(v),
                  v => WebhookSecretEncryptor.Decrypt(v));
        });

        builder.Property(m => m.WebhookSecretRotatedAtUtc);

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

        builder.Property(m => m.AutoCapture).IsRequired();
        builder.Property(m => m.RejectionReason).HasMaxLength(500);

        builder.OwnsOne(m => m.SettlementInfo, s =>
        {
            s.Property(v => v.BankAccountName).HasColumnName("SettlementBankAccountName").HasMaxLength(200);
            s.Property(v => v.BankAccountNumber).HasColumnName("SettlementBankAccountNumber").HasMaxLength(50);
            s.Property(v => v.BankName).HasColumnName("SettlementBankName").HasMaxLength(200);
            s.Property(v => v.SettlementCurrency).HasColumnName("SettlementCurrency").HasMaxLength(3);
            s.Property(v => v.SettlementSchedule).HasColumnName("SettlementSchedule").HasMaxLength(20);
        });

        builder.OwnsOne(m => m.ContactDetails, c =>
        {
            c.Property(v => v.Phone).HasColumnName("ContactPhone").HasMaxLength(30);
            c.Property(v => v.Address).HasColumnName("ContactAddress").HasMaxLength(500);
            c.Property(v => v.ContactPerson).HasColumnName("ContactPerson").HasMaxLength(200);
        });

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt);

        builder.Ignore(m => m.DomainEvents);
    }
}