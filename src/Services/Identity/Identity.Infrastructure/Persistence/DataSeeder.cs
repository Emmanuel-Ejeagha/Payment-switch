using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, IConfiguration configuration)
    {
        var adminEmail = (configuration["Seed:AdminEmail"] ?? "admin@paymentswitch.com")
            .Trim().ToLowerInvariant();
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "Seed:AdminPassword is not configured. The admin seeder requires a strong bootstrap password.");
        }

        var existing = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == adminEmail);

        if (existing is not null)
        {
            if (existing.Roles.Contains("Admin"))
            {
                return;
            }

            // The seed email is already registered by someone else. Handing out
            // the Admin role here would let anyone who registers the address
            // first take over the bootstrap admin, so promotion requires proof
            // of the bootstrap secret and anything else fails startup loudly.
            if (!BCrypt.Net.BCrypt.Verify(adminPassword, existing.PasswordHash.Hash))
            {
                throw new InvalidOperationException(
                    "Seed admin email is already registered by an unknown owner. Refusing to grant the Admin role.");
            }

            existing.AddRole("Admin");
            existing.MarkEmailConfirmed();
            dbContext.Entry(existing).Property("Roles").IsModified = true;
            await dbContext.SaveChangesAsync();
            return;
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        var user = new User(Guid.NewGuid(), new Email(adminEmail), new PasswordHash(hash), new FullName("Admin User"));
        user.AddRole("Admin");
        user.MarkEmailConfirmed();

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }
}
