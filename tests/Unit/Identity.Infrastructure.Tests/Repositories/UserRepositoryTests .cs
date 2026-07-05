using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistUser()
    {
        var user = new User(Guid.NewGuid(), new Email("test@example.com"), new PasswordHash("hash"), new FullName("Test"));
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(user.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("test@example.com", retrieved.Email.Value);
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldReturnTrueForExistingEmail()
    {
        var user = new User(Guid.NewGuid(), new Email("exist@example.com"), new PasswordHash("hash"), new FullName("Exist"));
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        Assert.True(await _repository.ExistsByEmailAsync("exist@example.com"));
    }

    public void Dispose() => _context.Dispose();
}