using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Payment.Application.Features.Command.CapturePayment;
using Payment.Application.Features.Command.CreatePaymentIntent;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.CollectSubscriptionCycle;

public class CollectSubscriptionCycleHandler
{
    /// <summary>Collection attempts before the invoice is written off and the subscription canceled.</summary>
    public const int MaxAttempts = 4;

    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly IInvoiceRepository _invoices;
    private readonly CreatePaymentIntentHandler _createIntent;
    private readonly CapturePaymentHandler _capture;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ILogger<CollectSubscriptionCycleHandler> _logger;

    public CollectSubscriptionCycleHandler(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        IInvoiceRepository invoices,
        CreatePaymentIntentHandler createIntent,
        CapturePaymentHandler capture,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        ILogger<CollectSubscriptionCycleHandler> logger)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _invoices = invoices;
        _createIntent = createIntent;
        _capture = capture;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<Result<CollectSubscriptionCycleResponse>> Handle(
        CollectSubscriptionCycleCommand command,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.GetByIdAsync(command.SubscriptionId, cancellationToken);
        if (subscription is null)
            return PaymentErrors.SubscriptionNotFound(command.SubscriptionId);
        if (subscription.Status == SubscriptionStatus.Canceled)
            return PaymentErrors.SubscriptionAlreadyCanceled(command.SubscriptionId);

        var plan = await _plans.GetByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is null)
            return PaymentErrors.PlanNotFound(subscription.PlanId);

        // The invoice is keyed on the billing period, so a retried cycle reuses the same one.
        var invoice = await _invoices.GetBySubscriptionPeriodAsync(
            subscription.Id, subscription.CurrentPeriodStart, cancellationToken);

        if (invoice is null)
        {
            invoice = new Invoice(
                Guid.NewGuid(),
                subscription.MerchantId,
                subscription.CustomerId,
                subscription.Id,
                Invoice.NewCode(),
                plan.Amount,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd);

            await _invoices.AddAsync(invoice, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _dispatcher.DispatchAsync(invoice.DomainEvents, cancellationToken);

            _logger.LogInformation(
                "Issued invoice {InvoiceCode} for subscription {SubscriptionCode} covering {PeriodStart:o}",
                invoice.Code, subscription.Code, invoice.PeriodStart);
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            // A prior attempt collected but did not roll the period forward; finish the job.
            subscription.MarkCyclePaid(plan.Interval);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _dispatcher.DispatchAsync(subscription.DomainEvents, cancellationToken);

            return Success(subscription, invoice);
        }

        if (invoice.Status != InvoiceStatus.Open)
            return new Error("Payment.InvoiceNotCollectable",
                $"Invoice '{invoice.Code}' is '{invoice.Status}' and cannot be collected.");

        // Deterministic key: retries of the same cycle never create a second charge.
        var idempotencyKey = $"sub-{subscription.Id:N}-{invoice.PeriodStart:yyyyMMddHHmmss}";

        var intentResult = await _createIntent.Handle(
            new CreatePaymentIntentCommand(
                subscription.MerchantId,
                invoice.Amount.Amount,
                invoice.Amount.Currency,
                "Card",
                null,
                null,
                idempotencyKey,
                subscription.CardToken),
            cancellationToken);

        if (!intentResult.IsSuccess)
            return await RecordFailure(subscription, invoice, plan.Interval,
                intentResult.Errors.First().Message, null, cancellationToken);

        var intent = intentResult.Value!;
        var status = intent.Status;

        // Off-session cycles cannot complete a 3DS challenge, so treat it as a failed attempt.
        if (status == PaymentStatus.RequiresAction.Value)
            return await RecordFailure(subscription, invoice, plan.Interval,
                "The card requires authentication and cannot be charged off-session.", intent.IntentId, cancellationToken);

        if (status == PaymentStatus.Failed.Value)
            return await RecordFailure(subscription, invoice, plan.Interval,
                "The gateway declined the charge.", intent.IntentId, cancellationToken);

        // When the merchant runs manual capture the intent stops at Authorized; capture it here
        // so the invoice reflects settled funds rather than a held authorization.
        if (status == PaymentStatus.Authorized.Value)
        {
            var captureResult = await _capture.Handle(
                new CapturePaymentCommand(intent.IntentId, null, idempotencyKey), cancellationToken);

            if (!captureResult.IsSuccess)
                return await RecordFailure(subscription, invoice, plan.Interval,
                    captureResult.Errors.First().Message, intent.IntentId, cancellationToken);
        }

        invoice.MarkPaid(intent.IntentId);
        subscription.MarkCyclePaid(plan.Interval);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(invoice.DomainEvents, cancellationToken);
        await _dispatcher.DispatchAsync(subscription.DomainEvents, cancellationToken);

        _logger.LogInformation(
            "Collected invoice {InvoiceCode} for subscription {SubscriptionCode}; next billing {NextBillingAt:o}",
            invoice.Code, subscription.Code, subscription.NextBillingAt);

        return Success(subscription, invoice);
    }

    private async Task<Result<CollectSubscriptionCycleResponse>> RecordFailure(
        Domain.Entities.Subscription subscription,
        Invoice invoice,
        BillingInterval interval,
        string error,
        Guid? paymentIntentId,
        CancellationToken cancellationToken)
    {
        invoice.RecordFailedAttempt(error, paymentIntentId);

        if (invoice.AttemptCount >= MaxAttempts)
        {
            invoice.MarkUncollectible();
            subscription.Cancel();

            _logger.LogWarning(
                "Invoice {InvoiceCode} uncollectible after {Attempts} attempts; canceled subscription {SubscriptionCode}: {Error}",
                invoice.Code, invoice.AttemptCount, subscription.Code, error);
        }
        else
        {
            var retryAt = DateTime.UtcNow.Add(ComputeBackoff(invoice.AttemptCount));
            subscription.MarkPastDue(retryAt);

            _logger.LogWarning(
                "Collection failed for invoice {InvoiceCode} (attempt {Attempt}): {Error}; retrying at {RetryAt:o}",
                invoice.Code, invoice.AttemptCount, error, retryAt);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(invoice.DomainEvents, cancellationToken);
        await _dispatcher.DispatchAsync(subscription.DomainEvents, cancellationToken);

        return new CollectSubscriptionCycleResponse(
            subscription.Id,
            invoice.Id,
            invoice.Status,
            subscription.Status,
            invoice.PaymentIntentId,
            Collected: false,
            Error: error);
    }

    private static CollectSubscriptionCycleResponse Success(Domain.Entities.Subscription subscription, Invoice invoice) =>
        new(subscription.Id,
            invoice.Id,
            invoice.Status,
            subscription.Status,
            invoice.PaymentIntentId,
            Collected: true,
            Error: null);

    /// <summary>Dunning schedule: 1h, 12h, then 24h between retries.</summary>
    internal static TimeSpan ComputeBackoff(int attempt) => attempt switch
    {
        1 => TimeSpan.FromHours(1),
        2 => TimeSpan.FromHours(12),
        _ => TimeSpan.FromHours(24)
    };
}
