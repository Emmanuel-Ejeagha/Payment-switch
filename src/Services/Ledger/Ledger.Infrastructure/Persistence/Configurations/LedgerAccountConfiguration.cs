using Ledger.Domain;
using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Infrastructure.Persistence.Configurations;

public class LedgerAccountConfiguration : IEntityTypeConfiguration<LedgerAccount>
{
    public void Configure(EntityTypeBuilder<LedgerAccount> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.MerchantId).IsRequired();
        builder.Property(a => a.AvailableBalance).IsRequired();
        builder.Property(a => a.PendingBalance).IsRequired();
        builder.Property(a => a.ReservedBalance).IsRequired();
        builder.Property(a => a.Currency).IsRequired().HasMaxLength(3);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasIndex(a => new { a.MerchantId, a.Currency }).IsUnique();

        builder.ToTable("LedgerAccounts", t => t.HasCheckConstraint(
            "CK_LedgerAccounts_NonNegativeBalances",
            "\"AvailableBalance\" >= 0 AND \"PendingBalance\" >= 0 AND \"ReservedBalance\" >= 0"));

        builder.Navigation(a => a.Journal)
            .UsePropertyAccessMode(PropertyAccessMode.PreferField);

        builder.OwnsMany(a => a.Journal, j =>
        {
            j.UsePropertyAccessMode(PropertyAccessMode.PreferField);
            j.WithOwner().HasForeignKey("LedgerAccountId");
            j.HasKey("Id");
            j.HasIndex("LedgerAccountId", "Timestamp");
            j.Property(e => e.Id).ValueGeneratedNever();
            j.Property(e => e.Type).HasConversion<string>().IsRequired();
            j.Property(e => e.DebitAccount).HasConversion<string>().IsRequired();
            j.Property(e => e.CreditAccount).HasConversion<string>().IsRequired();
            j.Property(e => e.Description).IsRequired().HasMaxLength(500);
            j.OwnsOne(e => e.Amount, a =>
            {
                a.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
                a.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
            });
            j.OwnsOne(e => e.CorrelationId, c =>
            {
                c.Property(cid => cid.Value).HasColumnName("CorrelationId").IsRequired().HasMaxLength(200);
                c.HasIndex(cid => cid.Value).IsUnique();
            });
            j.Property(e => e.Timestamp).IsRequired();
            j.ToTable("JournalEntries");
        });

        builder.Ignore(a => a.DomainEvents);
    }
}