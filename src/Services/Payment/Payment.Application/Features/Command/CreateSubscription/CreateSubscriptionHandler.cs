using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain;
using Payment.Domain.Entities;

namespace Payment.Application.Features.Command.CreateSubscription;

public class CreateSubscriptionHandler
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlanRepository _plans;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSubscriptionCommand> _validator;
    private readonly ILogger<CreateSubscriptionHandler> _logger;

    public CreateSubscriptionHandler(
        ISubscriptionRepository subscriptions,
        IPlanRepository plans,
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IValidator<CreateSubscriptionCommand> validator,
        ILogger<CreateSubscriptionHandler> logger)
    {
        _subscriptions = subscriptions;
        _plans = plans;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> Handle(CreateSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CreateSubscriptionCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var customer = await _customers.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null || customer.Deleted || customer.MerchantId != command.MerchantId)
            return PaymentErrors.CustomerNotFound(command.CustomerId);

        var plan = await _plans.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null || plan.MerchantId != command.MerchantId)
            return PaymentErrors.PlanNotFound(command.PlanId);
        if (!plan.Active)
            return PaymentErrors.PlanInactive(command.PlanId);

        var subscription = new Subscription(
            Guid.NewGuid(),
            command.MerchantId,
            command.CustomerId,
            command.PlanId,
            Subscription.NewCode(),
            command.CardToken,
            plan.Interval,
            command.StartAt);

        await _subscriptions.AddAsync(subscription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created subscription {Code} on plan {PlanCode}", subscription.Code, plan.Code);

        return subscription.ToDto();
    }
}
