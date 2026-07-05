using Ledger.Domain;
using Ledger.Domain.Entities;
using Ledger.Infrastructure.Inbox;
using Ledger.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<LedgerAccount> LedgerAccounts { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}