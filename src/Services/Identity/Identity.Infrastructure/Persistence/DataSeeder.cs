using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        if (await dbContext.Users.AnyAsync(u => u.Email.Value == "admin@paymentswitch.com"))
            return;

        var hash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        var user = new User(Guid.NewGuid(), new Email("admin@paymentswitch.com"), new PasswordHash(hash), new FullName("Admin User"));
        user.AddRole("Admin");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }
}
