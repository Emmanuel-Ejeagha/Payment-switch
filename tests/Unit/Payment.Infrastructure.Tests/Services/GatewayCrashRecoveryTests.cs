using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CapturePayment;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;
using Payment.Infrastructure.Services.Gateways;

namespace Payment.Infrastructure.Tests.Services;

/// <summary>
/// Proves the crash window between gateway success and local commit cannot
/// double-charge: a retry with the same idempotency key re-contacts the
/// gateway (the local transaction was lost) but the acquirer executes once.
/// </summary>
public class GatewayCrashRecoveryTests
{
    private sealed class CountingProvider : IPaymentGatewayProvider
    {
        public string Name => "counting";

        private readonly StripeMockGatewayProvider _inner = new();

        public readonly Dictionary<string, List<string?>> RefsByKey = new();

        public int CallsFor(string key) => RefsByKey.TryGetValue(key, out var list) ? list.Count : 0;

        public Task<GatewayResponse> CaptureAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            return Record(idempotencyKey, () => _inner.CaptureAsync(merchantId, gatewayRef, amount, idempotencyKey, cancellationToken));
        }

        public Task<GatewayResponse> AuthorizeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => _inner.AuthorizeAsync(merchantId, amount, cardDetails, securityCode, idempotencyKey, cancellationToken);

        public Task<GatewayResponse> ConfirmChallengeAsync(Guid merchantId, Money amount, CardDetails? cardDetails, string gatewayReference, CardSecurityCode? securityCode = null, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => _inner.ConfirmChallengeAsync(merchantId, amount, cardDetails, gatewayReference, securityCode, idempotencyKey, cancellationToken);

        public Task<GatewayResponse> VoidAsync(Guid merchantId, GatewayReference gatewayRef, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => _inner.VoidAsync(merchantId, gatewayRef, idempotencyKey, cancellationToken);

        public Task<GatewayResponse> RefundAsync(Guid merchantId, GatewayReference gatewayRef, Money amount, string? idempotencyKey = null, CancellationToken cancellationToken = default)
            => _inner.RefundAsync(merchantId, gatewayRef, amount, idempotencyKey, cancellationToken);

        private async Task<GatewayResponse> Record(string? key, Func<Task<GatewayResponse>> call)
        {
            var response = await call();
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (!RefsByKey.TryGetValue(key, out var list))
                    RefsByKey[key] = list = new List<string?>();
                list.Add(response.GatewayReference);
            }
            return response;
        }
    }

    private sealed class PostRollbackRepository : IPaymentIntentRepository
    {
        private readonly Guid _merchantId = Guid.NewGuid();

        public Task<PaymentIntent?> GetByIdAsync(Guid intentId, CancellationToken cancellationToken = default)
        {
            // Fresh instance per call, exactly as a retry reads post-rollback
            // DB state: authorized, with no capture leg recorded.
            var intent = new PaymentIntent(Guid.NewGuid(), _merchantId, new Money(100, "USD"), new IdempotencyKey("create-key"), PaymentMethod.Card);
            intent.Authorize(new AuthorizationCode("AUTH-1"), new GatewayReference("GW-1"), "create-key");
            return Task.FromResult<PaymentIntent?>(intent);
        }

        public Task<PaymentIntent?> GetByIdempotencyKeyAsync(Guid merchantId, string idempotencyKey, CancellationToken cancellationToken = default)
            => Task.FromResult<PaymentIntent?>(null);

        public Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<PaymentIntentDto>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<PaymentIntentDto>());

        public Task<int> CountByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<List<PaymentIntent>> GetExpirableBatchAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<PaymentIntent>());
    }

    private sealed class CrashOnceUnitOfWork : IUnitOfWork
    {
        private bool _crashed;

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!_crashed)
            {
                _crashed = true;
                throw new InvalidOperationException("simulated crash between gateway success and commit");
            }
            return Task.FromResult(1);
        }

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PassValidator : AbstractValidator<CapturePaymentCommand>
    {
    }

    [Fact]
    public async Task CaptureCrashBetweenGatewayAndCommit_RetryWithSameKey_ChargesOnce()
    {
        var counting = new CountingProvider();
        var gateway = new ResilientPaymentGatewayService(
            new GatewayProviderRegistry(new IPaymentGatewayProvider[] { counting }),
            new GatewayRouter(),
            NullLogger<ResilientPaymentGatewayService>.Instance);
        var handler = new CapturePaymentHandler(
            new PostRollbackRepository(),
            gateway,
            new CrashOnceUnitOfWork(),
            new PassValidator(),
            NullLogger<CapturePaymentHandler>.Instance);
        var command = new CapturePaymentCommand(Guid.NewGuid(), null, "crash-key-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command));
        var retry = await handler.Handle(command);

        Assert.True(retry.IsSuccess);
        Assert.Equal("Captured", retry.Value!.Status);
        // The crash forces a second gateway contact, but the acquirer must
        // have executed exactly once: a single distinct reference.
        Assert.Equal(2, counting.CallsFor("crash-key-1"));
        Assert.Single(counting.RefsByKey["crash-key-1"].Distinct());
    }
}
