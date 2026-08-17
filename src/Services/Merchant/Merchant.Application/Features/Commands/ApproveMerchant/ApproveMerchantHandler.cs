using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.ApproveMerchant;

public class ApproveMerchantHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ApproveMerchantCommand> _validator;
    private readonly ILogger<ApproveMerchantHandler> _logger;

    public ApproveMerchantHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<ApproveMerchantCommand> validator,
        ILogger<ApproveMerchantHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ApproveMerchantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(ApproveMerchantCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        try
        {
            merchant.Approve();
        }
        catch (InvalidOperationException)
        {
            return MerchantErrors.InvalidStatusTransition(merchant.Status.Value, "Approved");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}