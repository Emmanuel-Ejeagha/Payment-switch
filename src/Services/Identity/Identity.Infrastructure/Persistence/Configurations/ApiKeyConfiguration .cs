using Identity.Domain;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever(); 
        builder.Property(k => k.KeyHash).IsRequired();
        builder.Property(k => k.Environment).IsRequired();
        builder.Property(k => k.CreatedAt);
        builder.Property(k => k.RevokedAt);

        builder.Property<Guid>("UserId").IsRequired();
    }
}