using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.CreatePlan;

public class CreatePlanHandler
{
    private readonly IPlanRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePlanCommand> _validator;
    private readonly ILogger<CreatePlanHandler> _logger;

    public CreatePlanHandler(
        IPlanRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreatePlanCommand> validator,
        ILogger<CreatePlanHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PlanDto>> Handle(CreatePlanCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(CreatePlanCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var plan = new Plan(
            Guid.NewGuid(),
            command.MerchantId,
            Plan.NewCode(),
            command.Name,
            new Money(command.Amount, command.Currency),
            new BillingInterval(command.IntervalUnit, command.IntervalCount),
            command.Description);

        await _repository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created plan {Code} for Merchant {MerchantId}", plan.Code, command.MerchantId);

        return plan.ToDto();
    }
}
