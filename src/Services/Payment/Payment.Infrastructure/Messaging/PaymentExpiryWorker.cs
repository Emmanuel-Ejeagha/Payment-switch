using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Messaging;

public class PaymentExpiryOptions
{
    public const string SectionName = "PaymentExpiry";

    /// <summary>How long an in-flight intent may live before it is auto-expired.</summary>
    public int TimeoutMinutes { get; set; } = 30;

    /// <summary>How often the expiry sweep runs.</summary>
    public int PollIntervalMinutes { get; set; } = 5;
}

/// <summary>
/// Periodic sweep that expires payment intents which never left the in-flight
/// states (Pending / RequiresAction / Processing) within the configured TTL.
/// A fresh scope per intent keeps one failure from poisoning the batch and the
/// idempotent <c>PaymentIntent.Expire()</c> transition makes replays safe.
/// </summary>
public class PaymentExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<PaymentExpiryOptions> _options;
    private readonly ILogger<PaymentExpiryWorker> _logger;
    private const int BatchSize = 50;

    public PaymentExpiryWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<PaymentExpiryOptions> options,
        ILogger<PaymentExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(
            _options.Value.PollIntervalMinutes > 0 ? _options.Value.PollIntervalMinutes : 5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment expiry sweep failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-_options.Value.TimeoutMinutes);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentIntentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var candidates = await repository.GetExpirableBatchAsync(cutoff, BatchSize, cancellationToken);
        if (candidates.Count == 0)
            return;

        _logger.LogInformation("Sweeping {Count} stale payment intent(s)", candidates.Count);

        foreach (var intent in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                using var intentScope = _scopeFactory.CreateScope();
                var repo = intentScope.ServiceProvider.GetRequiredService<IPaymentIntentRepository>();
                var uow = intentScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var current = await repo.GetByIdAsync(intent.Id, cancellationToken);
                if (current is null)
                    continue;

                current.Expire();
                await uow.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Expired payment intent {IntentId}", current.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire payment intent {IntentId}", intent.Id);
            }
        }
    }
}