using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.ReactivateMerchant;

public class ReactivateMerchantHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ReactivateMerchantCommand> _validator;
    private readonly ILogger<ReactivateMerchantHandler> _logger;

    public ReactivateMerchantHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<ReactivateMerchantCommand> validator,
        ILogger<ReactivateMerchantHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ReactivateMerchantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(ReactivateMerchantCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        try
        {
            merchant.Reactivate();
        }
        catch (InvalidOperationException)
        {
            return MerchantErrors.InvalidStatusTransition(merchant.Status.Value, "Active");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}