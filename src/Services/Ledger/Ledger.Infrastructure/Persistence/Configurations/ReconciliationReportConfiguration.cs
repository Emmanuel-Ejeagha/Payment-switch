using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ledger.Infrastructure.Persistence.Configurations;

public class ReconciliationReportConfiguration : IEntityTypeConfiguration<ReconciliationReport>
{
    public void Configure(EntityTypeBuilder<ReconciliationReport> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RunAtUtc).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().IsRequired();
        builder.Property(r => r.TotalAccounts).IsRequired();
        builder.Property(r => r.MismatchCount).IsRequired();

        builder.Navigation(r => r.Items)
            .UsePropertyAccessMode(PropertyAccessMode.PreferField);

        builder.OwnsMany(r => r.Items, item =>
        {
            item.UsePropertyAccessMode(PropertyAccessMode.PreferField);
            item.WithOwner().HasForeignKey("ReconciliationReportId");
            item.HasKey("Id");
            item.Property(e => e.Id).ValueGeneratedNever();
            item.Property(i => i.MerchantId).IsRequired();
            item.Property(i => i.Currency).IsRequired().HasMaxLength(3);
            item.Property(i => i.ExpectedAvailable).IsRequired();
            item.Property(i => i.ActualAvailable).IsRequired();
            item.Property(i => i.ExpectedPending).IsRequired();
            item.Property(i => i.ActualPending).IsRequired();
            item.Property(i => i.ExpectedReserved).IsRequired();
            item.Property(i => i.ActualReserved).IsRequired();
            item.Property(i => i.IsMatch).IsRequired();
            item.ToTable("ReconciliationItems");
        });

        builder.HasIndex(r => r.RunAtUtc);
        builder.ToTable("ReconciliationReports");
        builder.Ignore(r => r.DomainEvents);
    }
}