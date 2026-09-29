using BuildingBlocks.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Persistence.Repositories;
using Payment.Infrastructure.Services.Gateways;

namespace Payment.API.IntegrationTests;

/// <summary>
/// Proves duplicate submits with the same idempotency key converge: two
/// parallel creates race on the unique (MerchantId, IdempotencyKey) index and
/// both callers receive the single winner instead of a 500.
/// </summary>
public class ConcurrentCreateTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;

    public ConcurrentCreateTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    private sealed class ActiveMerchantService : IMerchantService
    {
        public Task<Result<string>> GetMerchantStatusAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<string>.Success("Active"));

        public Task<Result<MerchantConfig>> GetMerchantConfigAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<MerchantConfig>.Success(new MerchantConfig(null, false)));

        public Task<Result<Guid?>> GetMerchantOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Guid?>.Success(null));

        public Task<Result<MerchantKeyResolution>> ResolveApiKeyAsync(string keyPrefix, string keyValue, CancellationToken cancellationToken = default)
            => Task.FromResult(Result<MerchantKeyResolution>.Failure(new Error("Test", "unused")));
    }

    private sealed class UnusedCardTokenRepository : ICardTokenRepository
    {
        public Task<CardToken?> GetByTokenAsync(Guid merchantId, string token, CancellationToken cancellationToken = default)
            => throw new NotImplementedException("Card tokens are not used in this test.");

        public Task AddAsync(CardToken cardToken, CancellationToken cancellationToken = default)
            => throw new NotImplementedException("Card tokens are not used in this test.");
    }

    private CreatePaymentIntentHandler CreateHandler(AppDbContext db)
    {
        var providers = new IPaymentGatewayProvider[] { new StripeMockGatewayProvider() };
        var gateway = new ResilientPaymentGatewayService(
            new GatewayProviderRegistry(providers),
            new GatewayRouter(),
            NullLogger<ResilientPaymentGatewayService>.Instance);
        return new CreatePaymentIntentHandler(
            new PaymentIntentRepository(db),
            gateway,
            new ActiveMerchantService(),
            new UnusedCardTokenRepository(),
            new Payment.Infrastructure.Persistence.UnitOfWork(db),
            new CreatePaymentIntentCommandValidator(),
            NullLogger<CreatePaymentIntentHandler>.Instance);
    }

    [Fact]
    public async Task ParallelCreates_SameIdempotencyKey_ConvergeOnSingleIntent()
    {
        var merchantId = Guid.NewGuid();
        var key = $"race-{Guid.NewGuid():N}";

        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var handler1 = CreateHandler(scope1.ServiceProvider.GetRequiredService<AppDbContext>());
        var handler2 = CreateHandler(scope2.ServiceProvider.GetRequiredService<AppDbContext>());

        var results = await Task.WhenAll(
            handler1.Handle(new CreatePaymentIntentCommand(merchantId, 10000, "USD", "Card", "4242", "Visa", key)),
            handler2.Handle(new CreatePaymentIntentCommand(merchantId, 10000, "USD", "Card", "4242", "Visa", key)));

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(results[0].Value!.IntentId, results[1].Value!.IntentId);

        using var dbScope = _factory.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.PaymentIntents.CountAsync(i => i.MerchantId == merchantId));
    }
}
