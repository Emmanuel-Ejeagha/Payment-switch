using BuildingBlocks.Shared.Retention;
using Notification.Domain;
using Notification.Infrastructure.DeadLetter;
using Notification.Infrastructure.Outbox;
using Notification.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = Notification.Domain.Entities.Notification;
using NotificationPreferenceEntity = Notification.Domain.Entities.NotificationPreference;

namespace Notification.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<NotificationEntity> Notifications { get; set; }
    public DbSet<NotificationPreferenceEntity> NotificationPreferences { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }
    public DbSet<DeadLetterRecord> DeadLetterRecords { get; set; }
    public DbSet<ArchivedRecord> ArchivedRecords { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        RetentionModelConfiguration.ConfigureArchivedRecord(modelBuilder);
    }
}