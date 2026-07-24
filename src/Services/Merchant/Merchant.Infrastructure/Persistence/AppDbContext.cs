using Merchant.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Merchant.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<MerchantEntity> Merchants { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}