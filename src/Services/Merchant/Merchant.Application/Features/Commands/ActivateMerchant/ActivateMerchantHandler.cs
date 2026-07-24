using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Commands.ActivateMerchant;

public class ActivateMerchantHandler
{
    private readonly IMerchantRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<ActivateMerchantCommand> _validator;
    private readonly ILogger<ActivateMerchantHandler> _logger;

    public ActivateMerchantHandler(
        IMerchantRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<ActivateMerchantCommand> validator,
        ILogger<ActivateMerchantHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> Handle(ActivateMerchantCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(ActivateMerchantCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var merchant = await _repository.GetByIdAsync(command.MerchantId, cancellationToken);
        if (merchant == null)
            return MerchantErrors.MerchantNotFound(command.MerchantId);

        try
        {
            merchant.Activate();
        }
        catch (InvalidOperationException)
        {
            return MerchantErrors.InvalidStatusTransition(merchant.Status.Value, "Active");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(merchant.DomainEvents, cancellationToken);
        return Result.Success();
    }
}
