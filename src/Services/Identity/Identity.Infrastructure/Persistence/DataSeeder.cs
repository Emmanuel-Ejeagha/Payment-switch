using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, IConfiguration configuration)
    {
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@paymentswitch.com";
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
            if (!existing.Roles.Contains("Admin"))
            {
                existing.AddRole("Admin");
                dbContext.Entry(existing).Property("Roles").IsModified = true;
                await dbContext.SaveChangesAsync();
            }
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
