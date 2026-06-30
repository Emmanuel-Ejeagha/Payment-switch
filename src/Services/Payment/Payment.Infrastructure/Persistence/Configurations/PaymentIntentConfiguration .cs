using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Persistence.Configurations;

public class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MerchantId).IsRequired();

        builder.OwnsOne(p => p.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
            a.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
        });

        builder.OwnsOne(p => p.IdempotencyKey, ik =>
        {
            ik.Property(i => i.Value).HasColumnName("IdempotencyKey").IsRequired();
            ik.HasIndex(i => i.Value).IsUnique();
        });

        builder.OwnsOne(p => p.AuthorizationCode, ac =>
        {
            ac.Property(a => a.Value).HasColumnName("AuthorizationCode").HasMaxLength(100);
        });

        builder.OwnsOne(p => p.GatewayReference, gr =>
        {
            gr.Property(g => g.Value).HasColumnName("GatewayReference").HasMaxLength(100);
        });

        builder.Property(p => p.PaymentMethod)
            .HasConversion(v => v.Value, v => PaymentMethod.FromString(v))
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion(v => v.Value, v => ResolveStatus(v))
            .IsRequired();

        builder.OwnsOne(p => p.CardDetails, cd =>
        {
            cd.Property(c => c.LastFour).HasColumnName("CardLastFour").HasMaxLength(4);
            cd.Property(c => c.Brand).HasColumnName("CardBrand").HasMaxLength(50);
            cd.Property(c => c.Token).HasColumnName("CardToken");
        });

        builder.OwnsMany(p => p.Transactions, t =>
        {
            t.WithOwner().HasForeignKey("PaymentIntentId");
            t.HasKey("Id");
            t.Property(tx => tx.Type).HasConversion<string>().IsRequired();
            t.OwnsOne(tx => tx.Amount, txA =>
            {
                txA.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
                txA.Property(m => m.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
            });
            t.OwnsOne(tx => tx.GatewayReference, txGr =>
            {
                txGr.Property(g => g.Value).HasColumnName("TransactionGatewayRef").HasMaxLength(100);
            });
            t.Property(tx => tx.Timestamp);
            t.ToTable("Transactions");
        });

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt);

        builder.Ignore(p => p.DomainEvents);
    }

    private static PaymentStatus ResolveStatus(string value) => value switch
    {
        "Pending" => PaymentStatus.Pending,
        "Authorized" => PaymentStatus.Authorized,
        "Captured" => PaymentStatus.Captured,
        "PartiallyCaptured" => PaymentStatus.PartiallyCaptured,
        "Voided" => PaymentStatus.Voided,
        "Failed" => PaymentStatus.Failed,
        "PartiallyRefunded" => PaymentStatus.PartiallyRefunded,
        "FullyRefunded" => PaymentStatus.FullyRefunded,
        _ => throw new ArgumentException($"Unknown status: {value}")
    };
}