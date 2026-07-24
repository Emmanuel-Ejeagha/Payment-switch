using Notification.Domain;
using Notification.Infrastructure.Outbox;
using Notification.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<NotificationEntity> Notifications { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}