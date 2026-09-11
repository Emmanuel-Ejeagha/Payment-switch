using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Tests.Persistence;

public class DataSeederTests : IDisposable
{
    private const string AdminEmail = "admin@paymentswitch.com";
    private const string SeedPassword = "Seed-Password-123";

    private readonly AppDbContext _context;

    public DataSeederTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private static IConfiguration Config(string? password = SeedPassword) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = password,
            })
            .Build();

    private async Task<User> RegisterUserAsync(string email, string password)
    {
        var user = new User(
            Guid.NewGuid(),
            new Email(email),
            new PasswordHash(BCrypt.Net.BCrypt.HashPassword(password)),
            new FullName("Someone Else"));
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return user;
    }

    private async Task<User> GetAdminAsync() =>
        (await _context.Users.AsTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == AdminEmail))!;

    [Fact]
    public async Task SeedAsync_FreshDatabase_CreatesConfirmedAdmin()
    {
        await DataSeeder.SeedAsync(_context, Config());

        var admin = await GetAdminAsync();
        Assert.NotNull(admin);
        Assert.Contains("Admin", admin.Roles);
    }

    [Fact]
    public async Task SeedAsync_ExistingEmailWithWrongPassword_DoesNotGrantAdmin()
    {
        await RegisterUserAsync(AdminEmail, "Attacker-Password-456");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => DataSeeder.SeedAsync(_context, Config()));
        Assert.Contains("Refusing to grant the Admin role", ex.Message);

        var user = await GetAdminAsync();
        Assert.NotNull(user);
        Assert.DoesNotContain("Admin", user.Roles);
    }

    [Fact]
    public async Task SeedAsync_ExistingEmailWithMatchingPassword_PromotesToAdmin()
    {
        await RegisterUserAsync(AdminEmail, SeedPassword);

        await DataSeeder.SeedAsync(_context, Config());

        var user = await GetAdminAsync();
        Assert.NotNull(user);
        Assert.Contains("Admin", user.Roles);
    }

    [Fact]
    public async Task SeedAsync_ExistingAdmin_IsNoOp()
    {
        await DataSeeder.SeedAsync(_context, Config());

        // Second run must not throw and must keep exactly one admin.
        await DataSeeder.SeedAsync(_context, Config());

        Assert.Equal(1, await _context.Users.CountAsync());
        Assert.Contains("Admin", (await GetAdminAsync()).Roles);
    }

    [Fact]
    public async Task SeedAsync_MissingPassword_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => DataSeeder.SeedAsync(_context, Config(password: null)));
    }
}
