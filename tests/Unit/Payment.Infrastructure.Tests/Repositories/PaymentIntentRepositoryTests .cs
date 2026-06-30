using Microsoft.EntityFrameworkCore;
using Payment.Domain;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;

namespace Payment.Infrastructure.Tests.Repositories;

public class PaymentIntentRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PaymentIntentRepository _repository;

    public PaymentIntentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new PaymentIntentRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistPaymentIntent()
    {
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("key"), PaymentMethod.Card);
        await _repository.AddAsync(intent);
        await _context.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(intent.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(100m, retrieved.Amount.Amount);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ShouldFindExistingIntent()
    {
        var merchantId = Guid.NewGuid();
        var key = new IdempotencyKey("dup-key");
        var intent = new PaymentIntent(Guid.NewGuid(), merchantId, new Money(200, "USD"), key, PaymentMethod.Card);
        await _repository.AddAsync(intent);
        await _context.SaveChangesAsync();

        var found = await _repository.GetByIdempotencyKeyAsync(merchantId, "dup-key");
        Assert.NotNull(found);
        Assert.Equal(intent.Id, found.Id);
    }

    public void Dispose() => _context.Dispose();
}