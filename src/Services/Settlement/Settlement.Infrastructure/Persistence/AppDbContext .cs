using Microsoft.EntityFrameworkCore;
using Settlement.Domain.Entities;
using Settlement.Infrastructure.Outbox;

namespace Settlement.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<SettlementBatch> SettlementBatches { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}