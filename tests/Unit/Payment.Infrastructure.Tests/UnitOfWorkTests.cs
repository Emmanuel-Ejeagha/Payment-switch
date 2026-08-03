using BuildingBlocks.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Payment.Domain;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Tests;

public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChanges_WhenConcurrentUpdateDetected_ShouldThrowConcurrencyConflictException()
    {
        var dbName = Guid.NewGuid().ToString();
        var optionsA = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var optionsB = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), new Money(100, "USD"), new IdempotencyKey("key"), PaymentMethod.Card);

        await using var contextA = new AppDbContext(optionsA);
        await contextA.PaymentIntents.AddAsync(intent);
        await contextA.SaveChangesAsync();
        var unitOfWork = new UnitOfWork(contextA);

        await using var contextB = new AppDbContext(optionsB);
        var concurrent = await contextB.PaymentIntents.FindAsync(intent.Id);
        Assert.NotNull(concurrent);
        contextB.Entry(concurrent).Property(x => x.RowVersion).CurrentValue = intent.RowVersion + 1;
        await contextB.SaveChangesAsync();

        intent.Authorize(new AuthorizationCode("AUTH1"), new GatewayReference("GW-1"));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => unitOfWork.SaveChangesAsync());
    }
}
