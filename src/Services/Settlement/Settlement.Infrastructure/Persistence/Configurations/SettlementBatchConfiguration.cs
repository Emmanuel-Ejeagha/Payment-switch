using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Settlement.Domain.Entities;
using Settlement.Domain.ValueObjects;

namespace Settlement.Infrastructure.Persistence.Configurations;

public class SettlementBatchConfiguration : IEntityTypeConfiguration<SettlementBatch>
{
    public void Configure(EntityTypeBuilder<SettlementBatch> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.BatchDate).IsRequired();
        builder.Property(b => b.Status)
            .HasConversion(s => s.Value, s => SettlementStatus.FromString(s))
            .IsRequired();
        builder.Property(b => b.TotalAmount).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.CompletedAt);

        builder.OwnsMany(b => b.Payouts, p =>
        {
            p.WithOwner().HasForeignKey("SettlementBatchId");
            p.HasKey(x => x.Id);
            p.Property(x => x.MerchantId).IsRequired();
            p.OwnsOne(x => x.GrossVolume, g =>
            {
                g.Property(m => m.Amount).HasColumnName("GrossAmount").IsRequired();
                g.Property(m => m.Currency).HasColumnName("GrossCurrency").IsRequired().HasMaxLength(3);
            });
            p.OwnsOne(x => x.Fees, f =>
            {
                f.Property(m => m.Amount).HasColumnName("FeesAmount").IsRequired();
                f.Property(m => m.Currency).HasColumnName("FeesCurrency").IsRequired().HasMaxLength(3);
            });
            p.OwnsOne(x => x.NetAmount, n =>
            {
                n.Property(m => m.Amount).HasColumnName("NetAmount").IsRequired();
                n.Property(m => m.Currency).HasColumnName("NetCurrency").IsRequired().HasMaxLength(3);
            });
            p.Property(x => x.Currency).HasMaxLength(3);
            p.ToTable("Payouts");
        });

        builder.Ignore(b => b.DomainEvents);
    }
}