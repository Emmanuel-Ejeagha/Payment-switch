using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.CancelSubscription;

public class CancelSubscriptionHandler
{
    private readonly ISubscriptionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelSubscriptionHandler> _logger;

    public CancelSubscriptionHandler(
        ISubscriptionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CancelSubscriptionHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetByIdAsync(command.SubscriptionId, cancellationToken);
        if (subscription is null)
            return PaymentErrors.SubscriptionNotFound(command.SubscriptionId);

        if (subscription.Status == SubscriptionStatus.Canceled)
            return PaymentErrors.SubscriptionAlreadyCanceled(command.SubscriptionId);

        if (command.AtPeriodEnd)
            subscription.CancelAtEndOfPeriod();
        else
            subscription.Cancel();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Canceled subscription {SubscriptionId} (atPeriodEnd: {AtPeriodEnd})",
            command.SubscriptionId, command.AtPeriodEnd);

        return subscription.ToDto();
    }
}
