using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain;

namespace Payment.Application.Features.Command.ArchivePlan;

public class ArchivePlanHandler
{
    private readonly IPlanRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArchivePlanHandler> _logger;

    public ArchivePlanHandler(
        IPlanRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ArchivePlanHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PlanDto>> Handle(ArchivePlanCommand command, CancellationToken cancellationToken = default)
    {
        var plan = await _repository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return PaymentErrors.PlanNotFound(command.PlanId);

        plan.Archive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Archived plan {PlanId}", command.PlanId);

        return plan.ToDto();
    }
}
