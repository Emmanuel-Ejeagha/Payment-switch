using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").IsRequired().HasMaxLength(255);
            email.HasIndex(e => e.Value).IsUnique();
        });

        builder.OwnsOne(u => u.PasswordHash, ph =>
        {
            ph.Property(p => p.Hash).HasColumnName("PasswordHash").IsRequired();
        });

        builder.OwnsOne(u => u.FullName, fn =>
        {
            fn.Property(f => f.Value).HasColumnName("FullName").IsRequired().HasMaxLength(200);
        });

        builder.Property(u => u.Roles)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        builder.OwnsMany(u => u.RefreshTokens, rt =>
        {
            rt.WithOwner().HasForeignKey("UserId");
            rt.HasKey("Id");
            rt.Property(t => t.Value).IsRequired();
            rt.Property(t => t.ExpiresAt).IsRequired();
            rt.Property(t => t.IsRevoked);
            rt.ToTable("RefreshTokens");
        });

        builder.HasMany(u => u.ApiKeys)
           .WithOne()
           .HasForeignKey("UserId")
           .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.ApiKeys)
       .UsePropertyAccessMode(PropertyAccessMode.Field)
       .HasField("_apiKeys");

        builder.Property(u => u.IsActive).IsRequired();
        builder.Ignore(u => u.DomainEvents);
    }
}